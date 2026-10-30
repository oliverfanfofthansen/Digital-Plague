using System;
using System.Collections.Generic;
using DigitalPlague.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalPlague.UI
{
    /// <summary>Vector cartography, geographic picking, markers, and camera controls only.</summary>
    public sealed class WorldMapElement : VisualElement
    {
        private static readonly Color Cyan = new Color(.35f, .77f, .79f);
        private static readonly Color Amber = new Color(.94f, .65f, .30f);
        private readonly Dictionary<string, Button> markers = new Dictionary<string, Button>();
        private readonly HashSet<string> playableIds = new HashSet<string>();
        private WorldSimulation world;
        private WorldMapGeometry geometry;
        private Action<string> selected;
        private string selectedId;
        private Vector2 pan;
        private Vector2 pointerStart;
        private Vector2 dragStartPan;
        private int capturedPointer = -1;
        private bool dragged;
        private readonly bool interactive;

        public float ZoomLevel { get; private set; } = 1;
        public Vector2 PanOffset => pan;
        public bool ShowConnections { get; set; } = true;
        public bool HighContrast { get; set; }
        public float ZoomSensitivity { get; set; } = 1;
        private float Scale => Mathf.Max(0.01f, Mathf.Min(contentRect.width / 360, contentRect.height / 180) * .94f) * ZoomLevel;
        private Vector2 Center => contentRect.center + pan;

        public WorldMapElement(bool interactive = true)
        {
            this.interactive = interactive;
            AddToClassList("world-map");
            focusable = interactive;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(evt => Refresh(selectedId));
            if (!interactive) return;
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(evt => capturedPointer = -1);
        }

        public void Configure(WorldSimulation simulation, WorldMapGeometry mapGeometry, Action<string> onSelected)
        {
            world = simulation;
            geometry = mapGeometry;
            selected = onSelected;
            Clear();
            markers.Clear();
            playableIds.Clear();
            foreach (LocationState location in world.Locations)
            {
                string id = location.Data.Id;
                playableIds.Add(id);
                if (!interactive) continue;
                var marker = new Button(() => selected?.Invoke(id))
                {
                    name = "location-" + id,
                    tooltip = "Inspect " + location.Data.DisplayName,
                    text = ""
                };
                marker.AddToClassList("map-marker");
                var label = new Label(location.Data.DisplayName.ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("marker-name");
                // Keep neighboring European labels on opposite sides of their anchors.
                if (id == "GBR") { label.style.left = -126; label.style.top = -18; }
                if (id == "DEU") { label.style.left = 24; label.style.top = 15; }
                if (id == "CHN") label.style.top = -18;
                if (id == "JPN") label.style.left = -48;
                marker.Add(label);
                markers.Add(id, marker);
                Add(marker);
            }
            Refresh(null);
        }

        public void Refresh(string locationId)
        {
            selectedId = locationId;
            if (world == null) return;
            foreach (LocationState location in world.Locations)
            {
                if (!markers.TryGetValue(location.Data.Id, out Button marker)) continue;
                Vector2 point = Project(location.Data.Coordinates);
                marker.style.left = point.x - 9;
                marker.style.top = point.y - 9;
                marker.EnableInClassList("selected", location.Data.Id == selectedId);
                marker.EnableInClassList("influenced", location.IsInfluenced);
                marker.EnableInClassList("origin", location.Data.Id == world.OriginId);
            }
            MarkDirtyRepaint();
        }

        public void ResetView()
        {
            ZoomLevel = 1;
            pan = Vector2.zero;
            Refresh(selectedId);
        }

        public void ZoomAt(float factor, Vector2 anchor)
        {
            float previousZoom = ZoomLevel;
            Vector2 previousCenter = Center;
            ZoomLevel = Mathf.Clamp(ZoomLevel * factor, 1, 4);
            pan = anchor - contentRect.center - (anchor - previousCenter) * (ZoomLevel / previousZoom);
            ClampPan();
            Refresh(selectedId);
        }

        public void ZoomIn() => ZoomAt(1.3f, contentRect.center);
        public void ZoomOut() => ZoomAt(1 / 1.3f, contentRect.center);

        public void PanBy(Vector2 amount)
        {
            pan += amount;
            ClampPan();
            Refresh(selectedId);
        }

        public Vector2 Project(GeoCoordinate coordinate)
        {
            return Center + new Vector2((float)coordinate.Longitude, -(float)coordinate.Latitude) * Scale;
        }

        private Vector2 Project(Vector2 longitudeLatitude) => Center + new Vector2(longitudeLatitude.x, -longitudeLatitude.y) * Scale;

        private void ClampPan()
        {
            float maxX = Mathf.Max(contentRect.width * .12f, (360 * Scale - contentRect.width) / 2 + 60);
            float maxY = Mathf.Max(contentRect.height * .12f, (180 * Scale - contentRect.height) / 2 + 40);
            pan.x = Mathf.Clamp(pan.x, -maxX, maxX);
            pan.y = Mathf.Clamp(pan.y, -maxY, maxY);
        }

        private void OnWheel(WheelEvent evt)
        {
            ZoomAt(Mathf.Exp(-evt.delta.y * .065f * ZoomSensitivity), evt.localMousePosition);
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;
            if (evt.button != 0 || target is Button || target?.GetFirstAncestorOfType<Button>() != null) return;
            Focus();
            pointerStart = evt.localPosition;
            dragStartPan = pan;
            capturedPointer = evt.pointerId;
            dragged = false;
            this.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (capturedPointer != evt.pointerId) return;
            Vector2 distance = (Vector2)evt.localPosition - pointerStart;
            if (distance.sqrMagnitude > 16) dragged = true;
            if (!dragged) return;
            pan = dragStartPan + distance;
            ClampPan();
            Refresh(selectedId);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (capturedPointer != evt.pointerId) return;
            if (!dragged) PickCountry(evt.localPosition);
            this.ReleasePointer(evt.pointerId);
            capturedPointer = -1;
        }

        private void PickCountry(Vector2 point)
        {
            Vector2 coordinates = (point - Center) / Scale;
            coordinates.y = -coordinates.y;
            foreach (CountryShape country in geometry.countries)
            {
                if (!playableIds.Contains(country.code)) continue;
                bool inside = false;
                foreach (MapRing ring in country.rings)
                    if (Contains(ring.points, coordinates)) inside = !inside;
                if (!inside) continue;
                selected?.Invoke(country.code);
                return;
            }
        }

        private static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private void Draw(MeshGenerationContext context)
        {
            if (geometry == null || world == null || contentRect.width < 1) return;
            Painter2D painter = context.painter2D;
            painter.lineWidth = .6f;
            painter.strokeColor = new Color(.25f, .43f, .47f, .18f);
            for (int longitude = -180; longitude <= 180; longitude += 15)
                Line(painter, Project(new Vector2(longitude, -90)), Project(new Vector2(longitude, 90)));
            for (int latitude = -90; latitude <= 90; latitude += 15)
                Line(painter, Project(new Vector2(-180, latitude)), Project(new Vector2(180, latitude)));

            foreach (CountryShape country in geometry.countries)
            {
                bool isSelected = interactive && country.code == selectedId;
                bool playable = interactive && playableIds.Contains(country.code);
                double influence = playable ? world.GetLocation(country.code).InfluenceFraction : 0;
                painter.fillColor = isSelected ? new Color(.23f, .23f, .16f, .95f)
                    : Color.Lerp(new Color(.045f, .105f, .135f), new Color(.08f, .37f, .39f), (float)Math.Sqrt(influence));
                painter.strokeColor = isSelected ? Amber
                    : playable ? new Color(.29f, .49f, .54f, HighContrast ? .95f : .8f)
                    : new Color(.23f, .37f, .42f, HighContrast ? .95f : .58f);
                painter.lineWidth = isSelected ? 1.5f : .65f;
                painter.BeginPath();
                foreach (MapRing ring in country.rings)
                {
                    if (ring.points.Length < 3) continue;
                    painter.MoveTo(Project(ring.points[0]));
                    for (int index = 1; index < ring.points.Length; index++) painter.LineTo(Project(ring.points[index]));
                    painter.ClosePath();
                }
                painter.Fill(FillRule.OddEven);
                painter.Stroke();
            }

            if (!interactive) return;
            if (ShowConnections)
            {
                foreach (NetworkLink link in world.Definition.Connections)
                {
                    LocationState from = world.GetLocation(link.From);
                    LocationState to = world.GetLocation(link.To);
                    Vector2 a = Project(from.Data.Coordinates);
                    Vector2 b = Project(to.Data.Coordinates);
                    bool active = from.IsInfluenced || to.IsInfluenced;
                    painter.strokeColor = active ? new Color(.35f, .77f, .79f, .48f) : new Color(.27f, .43f, .46f, .23f);
                    painter.lineWidth = active ? 1.1f : .7f;
                    painter.BeginPath();
                    painter.MoveTo(a);
                    Vector2 lift = Vector2.up * Mathf.Min(70, Vector2.Distance(a, b) * .16f);
                    painter.BezierCurveTo(Vector2.Lerp(a, b, .3f) + lift, Vector2.Lerp(a, b, .7f) + lift, b);
                    painter.Stroke();
                }
            }

            foreach (LocationState location in world.Locations)
            {
                if (!location.IsInfluenced) continue;
                Vector2 center = Project(location.Data.Coordinates);
                float radius = 15 + (float)Math.Sqrt(location.InfluenceFraction) * 20;
                painter.strokeColor = new Color(Cyan.r, Cyan.g, Cyan.b, .6f);
                painter.fillColor = new Color(Cyan.r, Cyan.g, Cyan.b, .08f);
                painter.lineWidth = 1;
                painter.BeginPath();
                painter.Arc(center, radius, 0, 360);
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
            }
        }

        private static void Line(Painter2D painter, Vector2 a, Vector2 b)
        {
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.Stroke();
        }
    }
}

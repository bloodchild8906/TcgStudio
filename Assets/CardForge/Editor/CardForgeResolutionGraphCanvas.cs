using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardForge.Editor
{
    public sealed class CardForgeResolutionGraphCanvas : VisualElement
    {
        readonly VisualElement surface = new VisualElement();
        readonly VisualElement edges = new VisualElement { pickingMode = PickingMode.Ignore };
        List<ResolutionGraphNode> nodes;
        List<ResolutionGraphLink> links;
        Action<ResolutionGraphNode> selected;
        Action<ResolutionGraphNode> moved;

        public CardForgeResolutionGraphCanvas()
        {
            AddToClassList("workflow-canvas");
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { name = "resolution-graph-scroll" };
            scroll.style.flexGrow = 1;
            Add(scroll);
            surface.AddToClassList("workflow-surface");
            scroll.Add(surface);
            edges.style.position = Position.Absolute;
            edges.style.left = 0;
            edges.style.top = 0;
            edges.generateVisualContent += DrawEdges;
        }

        public void Draw(List<ResolutionGraphNode> graphNodes, List<ResolutionGraphLink> graphLinks,
            string selectedId, Action<ResolutionGraphNode> onSelected, Action<ResolutionGraphNode> onMoved)
        {
            nodes = graphNodes ?? new List<ResolutionGraphNode>();
            links = graphLinks ?? new List<ResolutionGraphLink>();
            selected = onSelected;
            moved = onMoved;
            surface.Clear();
            surface.Add(edges);
            surface.style.width = Mathf.Max(980, nodes.Count == 0 ? 980 : nodes.Max(n => n.x) + 360);
            surface.style.height = Mathf.Max(560, nodes.Count == 0 ? 560 : nodes.Max(n => n.y) + 250);

            foreach (var node in nodes)
            {
                var current = node;
                var card = new VisualElement { name = "resolution-node-" + node.id };
                card.AddToClassList("workflow-node");
                card.AddToClassList("resolution-node");
                card.EnableInClassList("workflow-node-selected", node.id == selectedId);
                card.EnableInClassList("resolution-node-trigger", node.tone == "trigger");
                card.EnableInClassList("resolution-node-effect", node.tone == "effect");
                card.style.position = Position.Absolute;
                card.style.left = node.x;
                card.style.top = node.y;
                card.style.height = 112;
                var title = new Label(node.title ?? "Untitled") { name = "workflow-node-title" };
                var detail = new Label(node.detail ?? "") { name = "workflow-node-check" };
                var tone = new Label((node.tone ?? "process").ToUpperInvariant()) { name = "workflow-node-phase" };
                var flags = new Label((node.causesStateChange ? "STATE CHANGE" : "NO STATE CHANGE") + " · " + (node.emitsTriggers ? "EMITS TRIGGERS" : "NO TRIGGERS")) { name = "workflow-node-meta" };
                title.pickingMode = detail.pickingMode = tone.pickingMode = flags.pickingMode = PickingMode.Ignore;
                card.Add(tone); card.Add(title); card.Add(detail); card.Add(flags);

                bool dragging = false;
                int pointer = -1;
                Vector2 down = Vector2.zero;
                float startX = 0, startY = 0;
                card.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button != 0) return;
                    dragging = true; pointer = e.pointerId; down = e.position;
                    startX = current.x; startY = current.y;
                    card.CapturePointer(pointer);
                    selected?.Invoke(current);
                    foreach (var sibling in surface.Query<VisualElement>(className: "resolution-node").ToList())
                        sibling.EnableInClassList("workflow-node-selected", sibling == card);
                    e.StopPropagation();
                });
                card.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (!dragging || e.pointerId != pointer) return;
                    var delta = (Vector2)e.position - down;
                    current.x = Mathf.Max(20, startX + delta.x);
                    current.y = Mathf.Max(20, startY + delta.y);
                    if (!e.altKey) { current.x = Mathf.Round(current.x / 10) * 10; current.y = Mathf.Round(current.y / 10) * 10; }
                    card.style.left = current.x; card.style.top = current.y;
                    edges.MarkDirtyRepaint();
                    e.StopPropagation();
                });
                card.RegisterCallback<PointerUpEvent>(e =>
                {
                    if (!dragging || e.pointerId != pointer) return;
                    dragging = false; card.ReleasePointer(pointer); moved?.Invoke(current);
                    Draw(nodes, links, current.id, selected, moved);
                    e.StopPropagation();
                });
                surface.Add(card);
            }
            foreach (var link in links)
            {
                var from = nodes.FirstOrDefault(n => n.id == link.from);
                var to = nodes.FirstOrDefault(n => n.id == link.to);
                if (from == null || to == null) continue;
                var label = new Label(link.label ?? "next") { pickingMode = PickingMode.Ignore };
                label.AddToClassList("workflow-edge");
                label.style.position = Position.Absolute;
                label.style.left = (from.x + to.x + 220) / 2;
                label.style.top = (from.y + to.y) / 2 + 38;
                surface.Add(label);
            }
            edges.MarkDirtyRepaint();
        }

        void DrawEdges(MeshGenerationContext context)
        {
            if (nodes == null || links == null) return;
            var painter = context.painter2D;
            painter.lineWidth = 2;
            painter.strokeColor = new Color(.39f, .78f, .73f);
            foreach (var link in links)
            {
                var from = nodes.FirstOrDefault(n => n.id == link.from);
                var to = nodes.FirstOrDefault(n => n.id == link.to);
                if (from == null || to == null) continue;
                var a = new Vector2(from.x + 220, from.y + 56);
                var b = new Vector2(to.x, to.y + 56);
                var bend = Mathf.Max(70, Mathf.Abs(b.x - a.x) * .5f);
                painter.BeginPath(); painter.MoveTo(a); painter.BezierCurveTo(a + Vector2.right * bend, b - Vector2.right * bend, b); painter.Stroke();
                painter.BeginPath(); painter.MoveTo(b + new Vector2(-10, -5)); painter.LineTo(b); painter.LineTo(b + new Vector2(-10, 5)); painter.Stroke();
            }
        }
    }
}

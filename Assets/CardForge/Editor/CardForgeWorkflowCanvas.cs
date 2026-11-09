using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardForge.Editor
{
    public sealed class CardForgeWorkflowCanvas : VisualElement
    {
        public Func<string, string> ActionLabel;
        public Func<PhaseActionRule, string> ActionRuleLabel;
        readonly VisualElement surface = new VisualElement();
        readonly VisualElement edges = new VisualElement { pickingMode=PickingMode.Ignore };
        TurnFlowDocument flow;
        public CardForgeWorkflowCanvas()
        {
            AddToClassList("workflow-canvas");var scroll=new ScrollView(ScrollViewMode.VerticalAndHorizontal);scroll.style.flexGrow=1;Add(scroll);
            surface.AddToClassList("workflow-surface");scroll.Add(surface);
            edges.style.position=Position.Absolute;edges.style.left=0;edges.style.top=0;edges.style.width=Length.Percent(100);edges.style.height=Length.Percent(100);
            edges.generateVisualContent+=DrawEdges;
        }
        void DrawEdges(MeshGenerationContext context)
        {
            if(flow==null)return;var p=context.painter2D;p.lineWidth=2;p.strokeColor=new Color(.38f,.78f,.73f);
            foreach(var link in flow.links) {
                var from=flow.nodes.FirstOrDefault(n=>n.id==link.from);var to=flow.nodes.FirstOrDefault(n=>n.id==link.to);if(from==null||to==null)continue;
                var a=new Vector2(from.x+220,from.y+66);var b=new Vector2(to.x,to.y+66);float bend=Mathf.Max(70,Mathf.Abs(b.x-a.x)*.5f);
                p.BeginPath();p.MoveTo(a);p.BezierCurveTo(a+Vector2.right*bend,b-Vector2.right*bend,b);p.Stroke();
                p.BeginPath();p.MoveTo(b+new Vector2(-10,-5));p.LineTo(b);p.LineTo(b+new Vector2(-10,5));p.Stroke();
            }
        }
        public void Draw(TurnFlowDocument document,string selectedId,Action<TurnFlowNode> select,Action<TurnFlowNode> moved)
        {
            flow=document;surface.Clear();surface.Add(edges);if(flow==null)return;
            surface.style.width=Mathf.Max(980,flow.nodes.Count==0?980:flow.nodes.Max(n=>n.x)+350);
            surface.style.height=Mathf.Max(560,flow.nodes.Count==0?560:flow.nodes.Max(n=>n.y)+250);
            foreach(var node in flow.nodes) {
                var current=node;var card=new VisualElement();card.AddToClassList("workflow-node");card.EnableInClassList("workflow-node-selected",node.id==selectedId);
                card.style.position=Position.Absolute;card.style.left=node.x;card.style.top=node.y;card.style.height=132;
                foreach(var label in new[]{new Label((node.phase??"").ToUpperInvariant()){name="workflow-node-phase"},new Label(node.name){name="workflow-node-title"},new Label(node.check){name="workflow-node-check"},new Label(node.optional?"OPTIONAL":node.resolution){name="workflow-node-meta"}}){label.pickingMode=PickingMode.Ignore;card.Add(label);}
                var actions = new VisualElement { name = "workflow-node-actions", pickingMode = PickingMode.Ignore };
                var schedules = node.actionRules ?? new System.Collections.Generic.List<PhaseActionRule>();
                foreach (var schedule in schedules)
                {
                    var badge = new Label(ActionRuleLabel == null ? (ActionLabel == null ? schedule.actionId : ActionLabel(schedule.actionId)) : ActionRuleLabel(schedule)) { pickingMode = PickingMode.Ignore };
                    badge.AddToClassList("workflow-action-badge"); actions.Add(badge);
                }
                card.Add(actions);
                bool dragging=false;int pointer=-1;Vector2 down=Vector2.zero;float startX=0,startY=0;
                card.RegisterCallback<PointerDownEvent>(e=> {
                    if(e.button!=0)return;
                    foreach(var sibling in surface.Children())sibling.EnableInClassList("workflow-node-selected",sibling==card);
                    select?.Invoke(current);dragging=true;pointer=e.pointerId;down=e.position;startX=current.x;startY=current.y;card.CapturePointer(pointer);e.StopPropagation();
                });
                card.RegisterCallback<PointerMoveEvent>(e=> {
                    if(!dragging||e.pointerId!=pointer)return;var delta=(Vector2)e.position-down;
                    current.x=Mathf.Max(20,startX+delta.x);current.y=Mathf.Max(20,startY+delta.y);
                    if(!e.altKey){current.x=Mathf.Round(current.x/10)*10;current.y=Mathf.Round(current.y/10)*10;}
                    card.style.left=current.x;card.style.top=current.y;edges.MarkDirtyRepaint();e.StopPropagation();
                });
                card.RegisterCallback<PointerUpEvent>(e=>{if(!dragging||e.pointerId!=pointer)return;dragging=false;card.ReleasePointer(pointer);moved?.Invoke(current);Draw(flow,current.id,select,moved);e.StopPropagation();});
                surface.Add(card);
            }
            foreach(var link in flow.links) {
                var from=flow.nodes.FirstOrDefault(n=>n.id==link.from);var to=flow.nodes.FirstOrDefault(n=>n.id==link.to);if(from==null||to==null)continue;
                var label=new Label(link.label){pickingMode=PickingMode.Ignore};label.AddToClassList("workflow-edge");label.style.position=Position.Absolute;label.style.left=(from.x+to.x+220)/2;label.style.top=(from.y+to.y)/2+40;surface.Add(label);
            }
            edges.MarkDirtyRepaint();
        }
    }
}

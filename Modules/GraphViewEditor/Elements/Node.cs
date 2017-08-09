// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal class Node : GraphElement
    {
        protected virtual VisualContainer mainContainer { get; private set; }
        protected virtual VisualContainer leftContainer { get; private set; }
        protected virtual VisualContainer rightContainer { get; private set; }
        protected virtual VisualContainer titleContainer { get; private set; }
        protected virtual VisualContainer inputContainer { get; private set; }
        protected virtual VisualContainer outputContainer { get; private set; }

        private readonly Label m_TitleLabel;

        protected readonly Button m_CollapseButton;

        public override void SetPosition(Rect newPos)
        {
            if (ClassListContains("vertical"))
            {
                base.SetPosition(newPos);
            }
            else
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = newPos.x;
                style.positionTop = newPos.y;
            }
        }

        protected virtual void SetLayoutClassLists(NodePresenter nodePresenter)
        {
            if (ClassListContains("vertical") || ClassListContains("horizontal"))
            {
                return;
            }

            if (nodePresenter.orientation == Orientation.Vertical)
            {
                if (leftContainer.ContainsChild(titleContainer))
                {
                    leftContainer.RemoveChild(titleContainer);
                }
            }
            else
            {
                if (!leftContainer.ContainsChild(titleContainer))
                {
                    leftContainer.InsertChild(0, titleContainer);
                }
            }

            AddToClassList(nodePresenter.orientation == Orientation.Vertical ? "vertical" : "horizontal");
        }

        protected virtual void OnAnchorRemoved(NodeAnchor anchor)
        {}

        private void ProcessRemovedAnchors(IList<NodeAnchor> currentAnchors, VisualContainer anchorContainer, IList<NodeAnchorPresenter> currentPresenters)
        {
            foreach (var anchor in currentAnchors)
            {
                bool contains = false;
                var inputPres = anchor.GetPresenter<NodeAnchorPresenter>();
                foreach (var newPres in currentPresenters)
                {
                    if (newPres == inputPres)
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    OnAnchorRemoved(anchor);
                    anchorContainer.RemoveChild(anchor);
                }
            }
        }

        private void ProcessAddedAnchors(IList<NodeAnchor> currentAnchors, VisualContainer anchorContainer, IList<NodeAnchorPresenter> currentPresenters)
        {
            int index = 0;
            foreach (var newPres in currentPresenters)
            {
                bool contains = false;
                foreach (var currAnchor in currentAnchors)
                {
                    if (newPres == currAnchor.GetPresenter<NodeAnchorPresenter>())
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    anchorContainer.InsertChild(index, InstantiateNodeAnchor(newPres));
                }

                index++;
            }
        }

        public virtual NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter newPres)
        {
            return NodeAnchor.Create<EdgePresenter>(newPres);
        }

        private int ShowAnchors(bool show, IList<NodeAnchor> currentAnchors)
        {
            int count = 0;
            foreach (var anchor in currentAnchors)
            {
                NodeAnchorPresenter presenter = anchor.GetPresenter<NodeAnchorPresenter>();

                if ((show || presenter.connected) && !presenter.collapsed)
                {
                    anchor.visible = true;
                    anchor.RemoveFromClassList("hidden");
                    count++;
                }
                else
                {
                    anchor.visible = false;
                    anchor.AddToClassList("hidden");
                }
            }
            return count;
        }

        public void RefreshAnchors()
        {
            var nodePresenter = GetPresenter<NodePresenter>();

            var currentInputs = inputContainer.Query<NodeAnchor>().ToList();
            var currentOutputs = outputContainer.Query<NodeAnchor>().ToList();

            ProcessRemovedAnchors(currentInputs, inputContainer, nodePresenter.inputAnchors);
            ProcessRemovedAnchors(currentOutputs, outputContainer, nodePresenter.outputAnchors);

            ProcessAddedAnchors(currentInputs, inputContainer, nodePresenter.inputAnchors);
            ProcessAddedAnchors(currentOutputs, outputContainer, nodePresenter.outputAnchors);

            // Refresh the lists after all additions and everything took place
            currentInputs = inputContainer.Query<NodeAnchor>().ToList();
            currentOutputs = outputContainer.Query<NodeAnchor>().ToList();

            ShowAnchors(nodePresenter.expanded, currentInputs);
            int outputCount = ShowAnchors(nodePresenter.expanded, currentOutputs);

            // Show output container only if we have one or more child
            if (outputCount > 0)
            {
                if (!mainContainer.ContainsChild(rightContainer))
                {
                    mainContainer.AddChild(rightContainer);
                }
            }
            else
            {
                if (mainContainer.ContainsChild(rightContainer))
                {
                    mainContainer.RemoveChild(rightContainer);
                }
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodePresenter = GetPresenter<NodePresenter>();

            RefreshAnchors();

            m_TitleLabel.text = nodePresenter.title;

            m_CollapseButton.text = nodePresenter.expanded ? "collapse" : "expand";

            SetLayoutClassLists(nodePresenter);
        }

        protected virtual void ToggleCollapse()
        {
            var nodePresenter = GetPresenter<NodePresenter>();
            nodePresenter.expanded = !nodePresenter.expanded;
        }

        public Node()
        {
            usePixelCaching = true;
            mainContainer = new VisualContainer
            {
                name = "pane",
                pickingMode = PickingMode.Ignore,
            };
            leftContainer = new VisualContainer
            {
                name = "left",
                pickingMode = PickingMode.Ignore,
            };
            rightContainer = new VisualContainer
            {
                name = "right",
                pickingMode = PickingMode.Ignore,
            };
            titleContainer = new VisualContainer
            {
                name = "title",
                pickingMode = PickingMode.Ignore,
            };
            inputContainer = new VisualContainer
            {
                name = "input",
                pickingMode = PickingMode.Ignore,
            };
            outputContainer = new VisualContainer
            {
                name = "output",
                pickingMode = PickingMode.Ignore,
            };

            m_TitleLabel = new Label("");
            m_CollapseButton = new Button(ToggleCollapse)
            {
                text = "collapse"
            };

            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            AddChild(mainContainer);
            mainContainer.AddChild(leftContainer);
            mainContainer.AddChild(rightContainer);

            titleContainer.AddChild(m_TitleLabel);
            titleContainer.AddChild(m_CollapseButton);

            leftContainer.AddChild(inputContainer);
            rightContainer.AddChild(outputContainer);

            ClearClassList();
            AddToClassList("node");
        }
    }
}

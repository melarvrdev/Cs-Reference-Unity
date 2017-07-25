// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.Debugger
{
    class VisualTreeItem : TreeViewItem
    {
        public readonly VisualElement elt;

        public VisualTreeItem(VisualElement elt, int depth) : base((int)elt.controlid, depth, GetDisplayName(elt))
        {
            this.elt = elt;
        }

        private static string GetDisplayName(VisualElement elt)
        {
            return elt.GetType().Name + " " + elt.name;
        }

        public uint controlId { get { return elt.controlid; } }
    }

    class VisualTreeTreeView : TreeView
    {
        public VisualTreeTreeView(TreeViewState state)
            : base(state) {}

        public Panel panel;

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1);
            Recurse(root, panel.visualTree);
            return root;
        }

        private static void Recurse(TreeViewItem tree, VisualElement elt)
        {
            var child = new VisualTreeItem(elt, tree.depth + 1);
            tree.AddChild(child);
            var container = elt as VisualContainer;
            if (container != null)
            {
                for (int i = 0; i < container.childrenCount; i++)
                {
                    Recurse(child, container.GetChildAt(i));
                }
            }
        }

        public VisualTreeItem GetNodeFor(int selectedId)
        {
            return FindRows(new List<int> { selectedId }).FirstOrDefault() as VisualTreeItem;
        }
    }
}

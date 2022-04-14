// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options to display alternating background colors for collection view rows.
    /// </summary>
    /// <remarks>
    /// If there are more rows with content than there are visible rows, you will not see a difference between
    /// the <c>All</c> option and the <c>ContentOnly</c> option.
    /// </remarks>
    public enum AlternatingRowBackground
    {
        /// <summary>
        /// Do not alternate background colors for rows.
        /// </summary>
        None,
        /// <summary>
        /// Alternate background colors only for rows that have content. The background color does not alternate for empty lines.
        /// </summary>
        ContentOnly,
        /// <summary>
        /// Alternate background colors for all rows, regardless of whether they have content. The background color continues to alternate for empty lines.
        /// </summary>
        All
    }

    /// <summary>
    /// Options to change the virtualization method used by the collection view to display its content.
    /// </summary>
    public enum CollectionVirtualizationMethod
    {
        /// <summary>
        /// Collection view won't wait for the layout to update items, as the all have the same height. <c>fixedItemHeight</c> Needs to be set. More performant but less flexible.
        /// </summary>
        FixedHeight,
        /// <summary>
        /// Collection view will use the actual height of every item when geometry changes. More flexible but less performant.
        /// </summary>
        DynamicHeight,
    }

    /// <summary>
    /// Base class for controls that display virtualized vertical content inside a scroll view.
    /// </summary>
    public abstract class BaseVerticalCollectionView : BindableElement, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseVerticalCollectionView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the BaseVerticalCollectionView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription m_FixedItemHeight = new UxmlIntAttributeDescription { name = "fixed-item-height", obsoleteNames = new[] { "itemHeight, item-height" }, defaultValue = s_DefaultItemHeight };
            private readonly UxmlEnumAttributeDescription<CollectionVirtualizationMethod> m_VirtualizationMethod = new UxmlEnumAttributeDescription<CollectionVirtualizationMethod> { name = "virtualization-method", defaultValue = CollectionVirtualizationMethod.FixedHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };
            private readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_HorizontalScrollingEnabled = new UxmlBoolAttributeDescription { name = "horizontal-scrolling", defaultValue = false };

            /// <summary>
            /// Returns an empty enumerable, because list views usually do not have child elements.
            /// </summary>
            /// <returns>An empty enumerable.</returns>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Initializes <see cref="BaseVerticalCollectionView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var itemHeight = 0;
                var view = (BaseVerticalCollectionView)ve;
                view.reorderable = m_Reorderable.GetValueFromBag(bag, cc);

                // Avoid setting itemHeight unless it's explicitly defined.
                // Setting itemHeight property will activate inline property mode.
                if (m_FixedItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    view.fixedItemHeight = itemHeight;
                }

                view.virtualizationMethod = m_VirtualizationMethod.GetValueFromBag(bag, cc);
                view.showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                view.selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                view.showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
                view.horizontalScrollingEnabled = m_HorizontalScrollingEnabled.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// Callback triggered when the user acts on a selection of one or more items, for example by double-clicking or pressing Enter.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items chosen.
        /// </remarks>
        [Obsolete("onItemsChosen is deprecated, use itemsChosen instead", false)]
        public event Action<IEnumerable<object>> onItemsChosen
        {
            add => itemsChosen += value;
            remove => itemsChosen -= value;
        }

        /// <summary>
        /// Callback triggered when the user acts on a selection of one or more items, for example by double-clicking or pressing Enter.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items chosen.
        /// </remarks>
        public event Action<IEnumerable<object>> itemsChosen;

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        [Obsolete("onSelectionChange is deprecated, use selectionChanged instead", false)]
        public event Action<IEnumerable<object>> onSelectionChange
        {
            add => selectionChanged += value;
            remove => selectionChanged -= value;
        }

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        public event Action<IEnumerable<object>> selectionChanged;

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item index or item indices selected.
        /// </remarks>
        [Obsolete("onSelectedIndicesChange is deprecated, use selectedIndicesChanged instead", false)]
        public event Action<IEnumerable<int>> onSelectedIndicesChange
        {
            add => selectedIndicesChanged += value;
            remove => selectedIndicesChanged -= value;
        }

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item index or item indices selected.
        /// </remarks>
        public event Action<IEnumerable<int>> selectedIndicesChanged;

        /// <summary>
        /// Called when an item is moved in the itemsSource.
        /// </summary>
        public event Action<int, int> itemIndexChanged;

        /// <summary>
        /// Called when the itemsSource is reassigned or changes size.
        /// </summary>
        public event Action itemsSourceChanged;

        // [GR] We can get rid of this once the InternalTreeView is removed.
        private Func<int, int> m_GetItemId;

        internal Func<int, int> getItemId
        {
            get => m_GetItemId;
            set
            {
                m_GetItemId = value;
                RefreshItems();
            }
        }

        /// <summary>
        /// The data source for collection items.
        /// </summary>
        /// <remarks>
        /// This list contains the items that the <see cref="BaseVerticalCollectionView"/> displays.
        /// </remarks>
        public IList itemsSource
        {
            get => viewController?.itemsSource;
            set => GetOrCreateViewController().itemsSource = value;
        }

        internal virtual bool sourceIncludesArraySize => false;

        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
        /// </summary>
        [Obsolete("makeItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Func<VisualElement> makeItem
        {
            get => throw new UnityException("makeItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("makeItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Callback for binding a data item to the visual element.
        /// </summary>
        [Obsolete("makeItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Action<VisualElement, int> bindItem
        {
            get => throw new UnityException("bindItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("bindItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Callback for unbinding a data item from the VisualElement.
        /// </summary>
        [Obsolete("makeItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Action<VisualElement, int> unbindItem
        {
            get => throw new UnityException("unbindItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("unbindItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Callback invoked when a <see cref="VisualElement"/> created via <see cref="makeItem"/> is no longer needed and will be destroyed.
        /// </summary>
        [Obsolete("makeItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Action<VisualElement> destroyItem
        {
            get => throw new UnityException("destroyItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("destroyItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Returns the content container for the <see cref="BaseVerticalCollectionView"/>. Because the BaseVerticalCollectionView
        /// control automatically manages its content, this always returns null.
        /// </summary>
        public override VisualElement contentContainer => null;

        private SelectionType m_SelectionType;

        /// <summary>
        /// Controls the selection type.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="SelectionType.Single"/>.
        /// When you set the collection view to disable selections, any current selection is cleared.
        /// </remarks>
        public SelectionType selectionType
        {
            get { return m_SelectionType; }
            set
            {
                m_SelectionType = value;
                if (m_SelectionType == SelectionType.None)
                {
                    ClearSelection();
                }
                else if (m_SelectionType == SelectionType.Single)
                {
                    if (m_SelectedIndices.Count > 1)
                    {
                        SetSelection(m_SelectedIndices.First());
                    }
                }
            }
        }

        /// <summary>
        /// Returns the selected item from the data source. If multiple items are selected, returns the first selected item.
        /// </summary>
        public object selectedItem => m_SelectedItems.Count == 0 ? null : m_SelectedItems.First();

        /// <summary>
        /// Returns the selected items from the data source. Always returns an enumerable, even if no item is selected, or a single
        /// item is selected.
        /// </summary>
        public IEnumerable<object> selectedItems => m_SelectedItems;

        /// <summary>
        /// Returns or sets the selected item's index in the data source. If multiple items are selected, returns the
        /// first selected item's index. If multiple items are provided, sets them all as selected.
        /// </summary>
        public int selectedIndex
        {
            get { return m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First(); }
            set { SetSelection(value); }
        }

        /// <summary>
        /// Returns the indices of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
        /// single item is selected.
        /// </summary>
        public IEnumerable<int> selectedIndices => m_SelectedIndices;

        internal List<int> currentSelectionIds => m_SelectedIds;

        static readonly List<ReusableCollectionItem> k_EmptyItems = new List<ReusableCollectionItem>();
        internal IEnumerable<ReusableCollectionItem> activeItems => m_VirtualizationController?.activeItems ?? k_EmptyItems;
        internal ScrollView scrollView => m_ScrollView;
        internal ListViewDragger dragger => m_Dragger;
        internal CollectionVirtualizationController virtualizationController => GetOrCreateVirtualizationController();

        /// <summary>
        /// The view controller for this view.
        /// </summary>
        public CollectionViewController viewController => m_ViewController;

        /// <summary>
        /// The computed pixel-aligned height for the list elements.
        /// </summary>
        /// <remarks>
        /// This value changes depending on the current panel's DPI scaling.
        /// </remarks>
        /// <seealso cref="fixedItemHeight"/>
        [Obsolete("resolvedItemHeight is deprecated and will be removed from the API.", false)]
        public float resolvedItemHeight => ResolveItemHeight();

        internal float ResolveItemHeight(float height = -1)
        {
            var dpiScaling = scaledPixelsPerPoint;
            height = height < 0 ? fixedItemHeight : height;
            return Mathf.Round(height * dpiScaling) / dpiScaling;
        }

        /// <summary>
        /// Enable this property to display a border around the collection view.
        /// </summary>
        /// <remarks>
        /// If set to true, a border appears around the ScrollView that the collection view uses internally.
        /// </remarks>
        public bool showBorder
        {
            get => m_ScrollView.ClassListContains(borderUssClassName);
            set => m_ScrollView.EnableInClassList(borderUssClassName, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the user can drag list items to reorder them.
        /// </summary>
        /// <remarks>
        /// The default values is <c>false.</c>
        /// Set this value to <c>true</c> to allow the user to drag and drop the items in the list. The collection view
        /// provides a default controller to allow standard behavior. It also automatically handles reordering
        /// the items in the data source.
        /// </remarks>
        public bool reorderable
        {
            get => m_Dragger?.dragAndDropController?.enableReordering ?? false;
            set
            {
                if (m_Dragger?.dragAndDropController == null)
                {
                    if (value)
                        InitializeDragAndDropController();

                    return;
                }

                var controller = m_Dragger.dragAndDropController;
                if (controller != null && controller.enableReordering != value)
                {
                    controller.enableReordering = value;
                    Rebuild();
                }
            }
        }

        private bool m_HorizontalScrollingEnabled;

        /// <summary>
        /// This property controls whether the collection view shows a horizontal scroll bar when its content
        /// does not fit in the visible area.
        /// </summary>
        public bool horizontalScrollingEnabled
        {
            get { return m_HorizontalScrollingEnabled; }
            set
            {
                if (m_HorizontalScrollingEnabled == value)
                    return;

                m_HorizontalScrollingEnabled = value;
                m_ScrollView.mode = (value ? ScrollViewMode.VerticalAndHorizontal : ScrollViewMode.Vertical);
            }
        }

        [SerializeField]
        private AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;

        /// <summary>
        /// This property controls whether the background colors of collection view rows alternate.
        /// Takes a value from the <see cref="AlternatingRowBackground"/> enum.
        /// </summary>
        public AlternatingRowBackground showAlternatingRowBackgrounds
        {
            get { return m_ShowAlternatingRowBackgrounds; }
            set
            {
                if (m_ShowAlternatingRowBackgrounds == value)
                    return;

                m_ShowAlternatingRowBackgrounds = value;
                RefreshItems();
            }
        }

        internal static readonly int s_DefaultItemHeight = 30;
        internal float m_FixedItemHeight = s_DefaultItemHeight;
        internal bool m_ItemHeightIsInline;
        CollectionVirtualizationMethod m_VirtualizationMethod;

        /// <summary>
        /// The virtualization method to use for this collection when a scroll bar is visible.
        /// Takes a value from the <see cref="CollectionVirtualizationMethod"/> enum.
        /// </summary>
        /// <remarks>
        /// The default values is <c>FixedHeight.</c>
        /// When using fixed height, you need to specify the <see cref="fixedItemHeight"/> property.
        /// Fixed height is more performant but offers less flexibility on content.
        /// When using <c>DynamicHeight</c>, the collection will wait for the actual height to be computed.
        /// Dynamic height is more flexible but less performant.
        /// </remarks>
        public CollectionVirtualizationMethod virtualizationMethod
        {
            get => m_VirtualizationMethod;
            set
            {
                var oldValue = m_VirtualizationMethod;
                m_VirtualizationMethod = value;
                if (oldValue != value)
                {
                    CreateVirtualizationController();
                    Rebuild();
                }
            }
        }

        /// <summary>
        /// The height of a single item in the list, in pixels.
        /// </summary>
        /// <remarks>
        /// This property must be set when using the <see cref="virtualizationMethod"/> is set to <c>FixedHeight</c>, for the collection view to function.
        /// </remarks>
        [Obsolete("itemHeight is deprecated, use fixedItemHeight instead.", false)]
        public int itemHeight
        {
            get => (int)fixedItemHeight;
            set => fixedItemHeight = value;
        }

        /// <summary>
        /// The height of a single item in the list, in pixels.
        /// </summary>
        /// <remarks>
        /// This property must be set when using the <see cref="virtualizationMethod"/> is set to <c>FixedHeight</c>, for the collection view to function.
        /// </remarks>
        public float fixedItemHeight
        {
            get => m_FixedItemHeight;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(fixedItemHeight), "Value needs to be positive for virtualization.");

                m_ItemHeightIsInline = true;
                if (Math.Abs(m_FixedItemHeight - value) > float.Epsilon)
                {
                    m_FixedItemHeight = value;
                    RefreshItems();
                }
            }
        }

        readonly ScrollView m_ScrollView;
        CollectionViewController m_ViewController;
        CollectionVirtualizationController m_VirtualizationController;
        KeyboardNavigationManipulator m_NavigationManipulator;

        [SerializeField]
        internal Vector2 m_ScrollOffset;

        // Persisted. It's why this can't be a HashSet(). :(
        [SerializeField]
        private readonly List<int> m_SelectedIds = new List<int>();

        // Not persisted! Just used for fast lookups of selected indices and object references.
        // This is to avoid also having a mapping from index/object ref to index for the entire
        // items source.
        private readonly List<int> m_SelectedIndices = new List<int>();
        private readonly List<object> m_SelectedItems = new List<object>();

        private float m_LastHeight;
        internal float lastHeight => m_LastHeight;

        private bool m_IsRangeSelectionDirectionUp;
        private ListViewDragger m_Dragger;

        internal const float ItemHeightUnset = -1;
        internal static CustomStyleProperty<int> s_ItemHeightProperty = new CustomStyleProperty<int>("--unity-item-height");

        // View controller callbacks
        Action<int, int> m_ItemIndexChangedCallback;
        Action m_ItemsSourceChangedCallback;

        private protected virtual void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableCollectionItem>();
        }

        internal CollectionVirtualizationController GetOrCreateVirtualizationController()
        {
            if (m_VirtualizationController == null)
                CreateVirtualizationController();

            return m_VirtualizationController;
        }

        internal void CreateVirtualizationController<T>() where T : ReusableCollectionItem, new()
        {
            switch (virtualizationMethod)
            {
                case CollectionVirtualizationMethod.FixedHeight:
                    m_VirtualizationController = new FixedHeightVirtualizationController<T>(this);
                    break;
                case CollectionVirtualizationMethod.DynamicHeight:
                    m_VirtualizationController = new DynamicHeightVirtualizationController<T>(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(virtualizationMethod), virtualizationMethod, $"Unsupported {nameof(virtualizationMethod)} virtualization");
            }
        }

        internal CollectionViewController GetOrCreateViewController()
        {
            if (m_ViewController == null)
            {
                SetViewController(CreateViewController());
            }

            return m_ViewController;
        }

        /// <summary>
        /// Creates the view controller for this view.
        /// Override this method in inheritors to change the controller type.
        /// </summary>
        /// <returns>The view controller.</returns>
        protected abstract CollectionViewController CreateViewController();

        /// <summary>
        /// Assigns the view controller for this view and registers all events required for it to function properly.
        /// </summary>
        /// <param name="controller">The controller to use with this view.</param>
        public virtual void SetViewController(CollectionViewController controller)
        {
            if (m_ViewController != null)
            {
                m_ViewController.itemIndexChanged -= m_ItemIndexChangedCallback;
                m_ViewController.itemsSourceChanged -= m_ItemsSourceChangedCallback;
                m_ViewController.Dispose();
                m_ViewController = null;
            }

            m_ViewController = controller;

            if (m_ViewController != null)
            {
                m_ViewController.SetView(this);
                m_ViewController.itemIndexChanged += m_ItemIndexChangedCallback;
                m_ViewController.itemsSourceChanged += m_ItemsSourceChangedCallback;
            }
        }

        internal virtual ListViewDragger CreateDragger()
        {
            return new ListViewDragger(this);
        }

        internal void InitializeDragAndDropController()
        {
            if (m_Dragger != null)
            {
                m_Dragger.UnregisterCallbacksFromTarget(true);
                m_Dragger.dragAndDropController = null;
                m_Dragger = null;
            }

            m_Dragger = CreateDragger();
            m_Dragger.dragAndDropController = CreateDragAndDropController();
        }

        internal abstract ICollectionDragAndDropController CreateDragAndDropController();

        internal void SetDragAndDropController(ICollectionDragAndDropController dragAndDropController)
        {
            m_Dragger ??= CreateDragger();
            m_Dragger.dragAndDropController = dragAndDropController;
        }

        //Used for unit testing
        internal ICollectionDragAndDropController GetDragAndDropController()
        {
            return m_Dragger?.dragAndDropController;
        }

        /// <summary>
        /// The USS class name for BaseVerticalCollectionView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the BaseVerticalCollectionView element. Any styling applied to
        /// this class affects every BaseVerticalCollectionView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-collection-view";
        /// <summary>
        /// The USS class name for BaseVerticalCollectionView elements with a border.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the BaseVerticalCollectionView element if the instance's
        /// <see cref="BaseVerticalCollectionView.showBorder"/> property is set to true. Any styling applied to this class
        /// affects every such BaseVerticalCollectionView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string borderUssClassName = ussClassName + "--with-border";
        /// <summary>
        /// The USS class name of item elements in BaseVerticalCollectionView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item element the BaseVerticalCollectionView contains. Any styling applied to
        /// this class affects every item element located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// The USS class name of the drag hover bar.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the bar that appears when an item element is dragged. The
        /// <see cref="BaseVerticalCollectionView.reorderable"/> property must be true in order for items to be dragged.
        /// Any styling applied to this class affects every BaseVerticalCollectionView located beside, or below the stylesheet in the
        /// visual tree.
        /// </remarks>
        public static readonly string dragHoverBarUssClassName = ussClassName + "__drag-hover-bar";
        /// <summary>
        /// The USS class name applied to an item element on drag hover.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the list element that is dragged. The <see cref="BaseVerticalCollectionView.reorderable"/>
        /// property must be set to true for items to be draggable. Any styling applied to this class affects
        /// every BaseVerticalCollectionView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemDragHoverUssClassName = itemUssClassName + "--drag-hover";
        /// <summary>
        /// The USS class name of selected item elements in the BaseVerticalCollectionView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every selected element in the BaseVerticalCollectionView. The <see cref="BaseVerticalCollectionView.selectionType"/>
        /// property decides if zero, one, or more elements can be selected. Any styling applied to
        /// this class affects every BaseVerticalCollectionView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemSelectedVariantUssClassName = itemUssClassName + "--selected";
        /// <summary>
        /// The USS class name for odd rows in the BaseVerticalCollectionView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every odd-numbered item in the BaseVerticalCollectionView when the
        /// <see cref="BaseVerticalCollectionView.showAlternatingRowBackground"/> property is set to <c>ContentOnly</c> or <c>All</c>.
        /// When the <c>showAlternatingRowBackground</c> property is set to either of those values, odd-numbered items
        /// are displayed with a different background color than even-numbered items. This USS class is used to differentiate
        /// odd-numbered items from even-numbered items. When the <c>showAlternatingRowBackground</c> property is set to
        /// <c>None</c>, the USS class is not added, and any styling or behavior that relies on it's invalidated.
        /// </remarks>
        public static readonly string itemAlternativeBackgroundUssClassName = itemUssClassName + "--alternative-background";
        /// <summary>
        /// The USS class name of the scroll view in the BaseVerticalCollectionView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the BaseVerticalCollectionView's scroll view. Any styling applied to
        /// this class affects every BaseVerticalCollectionView scroll view located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string listScrollViewUssClassName = ussClassName + "__scroll-view";

        internal static readonly string backgroundFillUssClassName = ussClassName + "__background";

        /// <summary>
        /// Creates a <see cref="BaseVerticalCollectionView"/> with all default properties.
        /// The <see cref="BaseVerticalCollectionView.itemSource"/> must all be set for the BaseVerticalCollectionView to function properly.
        /// </summary>
        public BaseVerticalCollectionView()
        {
            AddToClassList(ussClassName);

            selectionType = SelectionType.Single;
            m_ScrollOffset = Vector2.zero;

            m_ScrollView = new ScrollView();
            m_ScrollView.viewDataKey = "list-view__scroll-view";
            m_ScrollView.AddToClassList(listScrollViewUssClassName);
            m_ScrollView.verticalScroller.valueChanged += v => OnScroll(new Vector2(0, v));

            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            m_ScrollView.contentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ScrollView.contentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            hierarchy.Add(m_ScrollView);

            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform; // Scroll views with virtualized content shouldn't have the "view transform" optimization

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;

            m_ItemIndexChangedCallback = OnItemIndexChanged;
            m_ItemsSourceChangedCallback = OnItemsSourceChanged;
        }

        /// <summary>
        /// Constructs a <see cref="BaseVerticalCollectionView"/>, with all required properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels. For <c>FixedHeight</c> virtualization only.</param>
        public BaseVerticalCollectionView(IList itemsSource, float itemHeight = ItemHeightUnset)
            : this()
        {
            if (Math.Abs(itemHeight - ItemHeightUnset) > float.Epsilon)
            {
                m_FixedItemHeight = itemHeight;
                m_ItemHeightIsInline = true;
            }

            if (itemsSource != null)
            {
                this.itemsSource = itemsSource;
            }
        }

        /// <summary>
        /// Constructs a <see cref="BaseVerticalCollectionView"/>, with all required properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels. For <c>FixedHeight</c> virtualization only.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        [Obsolete("makeItem and bindItem are now in ListView and TreeView directly, please use a constructor without these parameters.")]
        public BaseVerticalCollectionView(IList itemsSource, float itemHeight = ItemHeightUnset, Func<VisualElement> makeItem = null, Action<VisualElement, int> bindItem = null)
            : this()
        {
            if (Math.Abs(itemHeight - ItemHeightUnset) > float.Epsilon)
            {
                m_FixedItemHeight = itemHeight;
                m_ItemHeightIsInline = true;
            }

            this.itemsSource = itemsSource;
        }

        /// <summary>
        /// Gets the root element the specified TreeView item.
        /// </summary>
        /// <param name="id">The TreeView item identifier.</param>
        /// <returns>The TreeView item's root element.</returns>
        public VisualElement GetRootElementForId(int id)
        {
            return activeItems.FirstOrDefault(t => t.id == id)?.rootElement;
        }

        /// <summary>
        /// Gets the root element the specified collection view item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The item's root element.</returns>
        /// <remarks>
        /// This method provides an entry point to re-style elements added by Unity over the user-driven content.
        /// Ex. the drag handle in a ListView, or the Toggle in a TreeView.
        /// </remarks>
        public VisualElement GetRootElementForIndex(int index)
        {
            return GetRootElementForId(viewController.GetIdForIndex(index));
        }

        internal virtual bool HasValidDataAndBindings()
        {
            return m_ViewController != null && itemsSource != null;
        }

        void OnItemIndexChanged(int srcIndex, int dstIndex)
        {
            itemIndexChanged?.Invoke(srcIndex, dstIndex);
            if (!(binding is IInternalListViewBinding))
                RefreshItems();
            else
                schedule.Execute(RefreshItems).ExecuteLater(100);
        }

        void OnItemsSourceChanged()
        {
            itemsSourceChanged?.Invoke();
        }

        /// <summary>
        /// Rebinds a single item if it is currently visible in the collection view.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void RefreshItem(int index)
        {
            foreach (var recycledItem in activeItems)
            {
                if (recycledItem.index == index)
                {
                    viewController.InvokeBindItem(recycledItem, recycledItem.index);
                    break;
                }
            }
        }

        /// <summary>
        /// Rebinds all items currently visible.
        /// </summary>
        /// <remarks>
        /// Call this method whenever the data source changes.
        /// </remarks>
        public void RefreshItems()
        {
            using (new ProfilerMarker("BaseVerticalCollectionView.RefreshItems").Auto())
            {
                if (m_ViewController == null)
                    return;

                RefreshSelection();
                virtualizationController.Refresh(false);
                PostRefresh();
            }
        }

        [Obsolete("Refresh() has been deprecated. Use Rebuild() instead. (UnityUpgradable) -> Rebuild()", false)]
        public void Refresh()
        {
            Rebuild();
        }

        /// <summary>
        /// Clears the collection view, recreates all visible visual elements, and rebinds all items.
        /// </summary>
        /// <remarks>
        /// Call this method whenever a structural change is made to the view.
        /// </remarks>
        public void Rebuild()
        {
            using (new ProfilerMarker("BaseVerticalCollectionView.Rebuild").Auto())
            {
                if (m_ViewController == null)
                    return;

                RefreshSelection();
                virtualizationController.Refresh(true);
                PostRefresh();
            }
        }

        private void RefreshSelection()
        {
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            if (viewController?.itemsSource == null)
                return;

            // O(n)
            if (m_SelectedIds.Count > 0)
            {
                // Add selected objects to working lists.
                var count = viewController.itemsSource.Count;
                for (var index = 0; index < count; ++index)
                {
                    if (!m_SelectedIds.Contains(viewController.GetIdForIndex(index)))
                        continue;

                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(viewController.GetItemForIndex(index));
                }
            }
        }

        private protected virtual void PostRefresh()
        {
            if (!HasValidDataAndBindings())
                return;

            m_LastHeight = m_ScrollView.layout.height;

            if (float.IsNaN(m_ScrollView.layout.height))
                return;

            Resize(m_ScrollView.layout.size);
        }

        /// <summary>
        /// Scrolls to a specific VisualElement.
        /// </summary>
        /// <param name="visualElement">The element to scroll to.</param>
        public void ScrollTo(VisualElement visualElement)
        {
            m_ScrollView.ScrollTo(visualElement);
        }

        /// <summary>
        /// Scrolls to a specific item index and makes it visible.
        /// </summary>
        /// <param name="index">Item index to scroll to. Specify -1 to make the last item visible.</param>
        public void ScrollToItem(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            virtualizationController.ScrollToItem(index);
        }

        /// <summary>
        /// Scrolls to a specific item id and makes it visible.
        /// </summary>
        /// <param name="id">Item id to scroll to.</param>
        [Obsolete("ScrollToId() has been deprecated. Use ScrollToItemById() instead. (UnityUpgradable) -> ScrollToItemById(*)", false)]
        public void ScrollToId(int id)
        {
            ScrollToItemById(id);
        }

        /// <summary>
        /// Scrolls to a specific item id and makes it visible.
        /// </summary>
        /// <param name="id">Item id to scroll to.</param>
        public void ScrollToItemById(int id)
        {
            if (!HasValidDataAndBindings())
                return;

            var index = viewController.GetIndexForId(id);
            virtualizationController.ScrollToItem(index);
        }

        private void OnScroll(Vector2 offset)
        {
            if (!HasValidDataAndBindings())
                return;

            virtualizationController.OnScroll(offset);
        }

        private void Resize(Vector2 size, int layoutPass = -1)
        {
            virtualizationController.Resize(size, layoutPass);
            m_LastHeight = size.y;
            virtualizationController.UpdateBackground();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_ScrollView.contentContainer.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));
            m_ScrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_ScrollView.contentContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.contentContainer.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            m_ScrollView.contentContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            m_ScrollView.contentContainer.RemoveManipulator(m_NavigationManipulator);
            m_ScrollView.contentContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            m_ScrollView.contentContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.contentContainer.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            m_ScrollView.contentContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        // TODO: make private. This doesn't need to be in the public API. Unit tests can be implemented with SendEvent.
        // Obsoleted as error for 2022.2. We can remove completely in the next version.
        [Obsolete("OnKeyDown is obsolete and will be removed from ListView. Use the event system instead, i.e. SendEvent(EventBase e).", true)]
        public void OnKeyDown(KeyDownEvent evt)
        {
            m_NavigationManipulator.OnKeyDown(evt);
        }

        private bool Apply(KeyboardNavigationOperation op, bool shiftKey)
        {
            if (selectionType == SelectionType.None || !HasValidDataAndBindings())
            {
                return false;
            }

            void HandleSelectionAndScroll(int index)
            {
                if (index < 0 || index >= m_ViewController.itemsSource.Count)
                    return;

                if (selectionType == SelectionType.Multiple && shiftKey && m_SelectedIndices.Count != 0)
                {
                    DoRangeSelection(index);
                }
                else
                {
                    selectedIndex = index;
                }

                ScrollToItem(index);
            }

            switch (op)
            {
                case KeyboardNavigationOperation.SelectAll:
                    SelectAll();
                    return true;
                case KeyboardNavigationOperation.Cancel:
                    ClearSelection();
                    return true;
                case KeyboardNavigationOperation.Submit:
                    itemsChosen?.Invoke(m_SelectedItems);
                    ScrollToItem(selectedIndex);
                    return true;
                case KeyboardNavigationOperation.Previous:
                    if (selectedIndex > 0)
                    {
                        HandleSelectionAndScroll(selectedIndex - 1);
                        return true;
                    }
                    break; // Allow focus to move outside the ListView
                case KeyboardNavigationOperation.Next:
                    if (selectedIndex + 1 < m_ViewController.itemsSource.Count)
                    {
                        HandleSelectionAndScroll(selectedIndex + 1);
                        return true;
                    }
                    break; // Allow focus to move outside the ListView
                case KeyboardNavigationOperation.Begin:
                    HandleSelectionAndScroll(0);
                    return true;
                case KeyboardNavigationOperation.End:
                    HandleSelectionAndScroll(m_ViewController.itemsSource.Count - 1);
                    return true;
                case KeyboardNavigationOperation.PageDown:
                    if (m_SelectedIndices.Count > 0)
                    {
                        var selectionDown = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Min() : m_SelectedIndices.Max();
                        HandleSelectionAndScroll(Mathf.Min(viewController.itemsSource.Count - 1, selectionDown + (virtualizationController.visibleItemCount - 1)));
                    }
                    return true;
                case KeyboardNavigationOperation.PageUp:
                    if (m_SelectedIndices.Count > 0)
                    {
                        var selectionUp = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Min() : m_SelectedIndices.Max();
                        HandleSelectionAndScroll(Mathf.Max(0, selectionUp - (virtualizationController.visibleItemCount - 1)));
                    }
                    return true;
            }

            return false;
        }

        private void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            var shiftKey = sourceEvent is KeyDownEvent kde && kde.shiftKey ||
                           sourceEvent is INavigationEvent ne && ne.shiftKey;
            if (Apply(op, shiftKey))
            {
                sourceEvent.StopPropagation();
                sourceEvent.PreventDefault();
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            // Support cases where PointerMove corresponds to a MouseDown or MouseUp event with multiple buttons.
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) == 0)
                {
                    ProcessPointerUp(evt);
                }
                else
                {
                    ProcessPointerDown(evt);
                }
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.pointerType != PointerType.mouse)
            {
                ProcessPointerDown(evt);
                panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }
            else
            {
                ProcessPointerDown(evt);
            }
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            ClearSelection();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerType != PointerType.mouse)
            {
                ProcessPointerUp(evt);
                panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }
            else
            {
                ProcessPointerUp(evt);
            }
        }

        private Vector3 m_TouchDownPosition;

        private void ProcessPointerDown(IPointerEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                m_TouchDownPosition = evt.position;
                return;
            }

            DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
        }

        private void ProcessPointerUp(IPointerEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                var delta = evt.position - m_TouchDownPosition;
                if (delta.sqrMagnitude <= ScrollView.ScrollThresholdSquared)
                {
                    DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
                }
            }
            else
            {
                var clickedIndex = virtualizationController.GetIndexFromPosition(evt.localPosition);
                if (selectionType == SelectionType.Multiple
                    && !evt.shiftKey
                    && !evt.actionKey
                    && m_SelectedIndices.Count > 1
                    && m_SelectedIndices.Contains(clickedIndex))
                {
                    ProcessSingleClick(clickedIndex);
                }
            }
        }

        private void DoSelect(Vector2 localPosition, int clickCount, bool actionKey, bool shiftKey)
        {
            var clickedIndex = virtualizationController.GetIndexFromPosition(localPosition);
            if (clickedIndex > viewController.itemsSource.Count - 1)
                return;

            if (selectionType == SelectionType.None)
                return;

            var clickedItemId = viewController.GetIdForIndex(clickedIndex);
            switch (clickCount)
            {
                case 1:
                    if (selectionType == SelectionType.Multiple && actionKey)
                    {
                        // Add/remove single clicked element
                        if (m_SelectedIds.Contains(clickedItemId))
                            RemoveFromSelection(clickedIndex);
                        else
                            AddToSelection(clickedIndex);
                    }
                    else if (selectionType == SelectionType.Multiple && shiftKey)
                    {
                        if (m_SelectedIndices.Count == 0)
                        {
                            SetSelection(clickedIndex);
                        }
                        else
                        {
                            DoRangeSelection(clickedIndex);
                        }
                    }
                    else if (selectionType == SelectionType.Multiple && m_SelectedIndices.Contains(clickedIndex))
                    {
                        // Do noting, selection will be processed OnPointerUp.
                        // If drag and drop will be started ListViewDragger will capture the mouse and ListView will not receive the mouse up event.
                    }
                    else // single
                    {
                        SetSelection(clickedIndex);
                    }

                    break;
                case 2:
                    if (itemsChosen != null)
                    {
                        ProcessSingleClick(clickedIndex);
                    }

                    itemsChosen?.Invoke(m_SelectedItems);
                    break;
            }
        }

        private void DoRangeSelection(int rangeSelectionFinalIndex)
        {
            var selectionOrigin = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Max() : m_SelectedIndices.Min();

            ClearSelectionWithoutValidation();

            // Add range
            var range = new List<int>();
            m_IsRangeSelectionDirectionUp = rangeSelectionFinalIndex < selectionOrigin;
            if (m_IsRangeSelectionDirectionUp)
            {
                for (var i = rangeSelectionFinalIndex; i <= selectionOrigin; i++)
                    range.Add(i);
            }
            else
            {
                for (var i = rangeSelectionFinalIndex; i >= selectionOrigin; i--)
                    range.Add(i);
            }

            AddToSelection(range);
        }

        private void ProcessSingleClick(int clickedIndex)
        {
            SetSelection(clickedIndex);
        }

        internal void SelectAll()
        {
            if (!HasValidDataAndBindings())
                return;

            if (selectionType != SelectionType.Multiple)
            {
                return;
            }

            for (var index = 0; index < m_ViewController.itemsSource.Count; index++)
            {
                var id = viewController.GetIdForIndex(index);
                var item = viewController.GetItemForIndex(index);

                foreach (var recycledItem in activeItems)
                    if (recycledItem.id == id)
                        recycledItem.SetSelected(true);

                if (!m_SelectedIds.Contains(id))
                {
                    m_SelectedIds.Add(id);
                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(item);
                }
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        /// <summary>
        /// Adds an item to the collection of selected items.
        /// </summary>
        /// <param name="index">Item index.</param>
        public void AddToSelection(int index)
        {
            AddToSelection(new[] { index });
        }

        internal void AddToSelection(IList<int> indexes)
        {
            if (!HasValidDataAndBindings() || indexes == null || indexes.Count == 0)
                return;

            foreach (var index in indexes)
            {
                AddToSelectionWithoutValidation(index);
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        private void AddToSelectionWithoutValidation(int index)
        {
            if (m_SelectedIndices.Contains(index))
                return;

            var id = viewController.GetIdForIndex(index);
            var item = viewController.GetItemForIndex(index);

            foreach (var recycledItem in activeItems)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(true);

            m_SelectedIds.Add(id);
            m_SelectedIndices.Add(index);
            m_SelectedItems.Add(item);
        }

        /// <summary>
        /// Removes an item from the collection of selected items.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void RemoveFromSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            RemoveFromSelectionWithoutValidation(index);
            NotifyOfSelectionChange();
            SaveViewData();
        }

        private void RemoveFromSelectionWithoutValidation(int index)
        {
            if (!m_SelectedIndices.Contains(index))
                return;

            var id = viewController.GetIdForIndex(index);
            var item = viewController.GetItemForIndex(index);

            foreach (var recycledItem in activeItems)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(false);

            m_SelectedIds.Remove(id);
            m_SelectedIndices.Remove(index);
            m_SelectedItems.Remove(item);
        }

        /// <summary>
        /// Sets the currently selected item.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void SetSelection(int index)
        {
            if (index < 0)
            {
                ClearSelection();
                return;
            }

            SetSelection(new[] { index });
        }

        /// <summary>
        /// Sets a collection of selected items.
        /// </summary>
        /// <param name="indices">The collection of the indices of the items to be selected.</param>
        public void SetSelection(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, true);
        }

        /// <summary>
        /// Sets a collection of selected items without triggering a selection change callback.
        /// </summary>
        /// <param name="indices">The collection of items to be selected.</param>
        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, false);
        }

        internal void SetSelectionInternal(IEnumerable<int> indices, bool sendNotification)
        {
            if (!HasValidDataAndBindings() || indices == null)
                return;

            ClearSelectionWithoutValidation();
            foreach (var index in indices)
                AddToSelectionWithoutValidation(index);

            if (sendNotification)
                NotifyOfSelectionChange();

            SaveViewData();
        }

        private void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings())
                return;

            selectionChanged?.Invoke(m_SelectedItems);
            selectedIndicesChanged?.Invoke(m_SelectedIndices);
        }

        /// <summary>
        /// Deselects any selected items.
        /// </summary>
        public void ClearSelection()
        {
            if (!HasValidDataAndBindings() || m_SelectedIds.Count == 0)
                return;

            ClearSelectionWithoutValidation();
            NotifyOfSelectionChange();
        }

        private void ClearSelectionWithoutValidation()
        {
            foreach (var recycledItem in activeItems)
                recycledItem.SetSelected(false);

            m_SelectedIds.Clear();
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();
            OverwriteFromViewData(this, key);
        }

        [EventInterest(typeof(PointerUpEvent), typeof(FocusEvent), typeof(NavigationSubmitEvent), typeof(BlurEvent))]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            // We always need to know when pointer up event occurred to reset DragEventsProcessor flags.
            // Some controls may capture the mouse, but the collection view is a composite root (isCompositeRoot),
            // and will always receive ExecuteDefaultAction despite what the actual event target is.
            if (evt.eventTypeId == PointerUpEvent.TypeId())
            {
                m_Dragger?.OnPointerUpEvent((PointerUpEvent)evt);
            }
            // We need to store the focused item in order to be able to scroll out and back to it, without
            // seeing the focus affected. To do so, we store the path to the tree element that is focused,
            // and set it back in Setup().
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                m_VirtualizationController?.OnFocus(evt.leafTarget as VisualElement);
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                BlurEvent e = evt as BlurEvent;
                m_VirtualizationController.OnBlur(e?.relatedTarget as VisualElement);
            }
            else if (evt.eventTypeId == NavigationSubmitEvent.TypeId())
            {
                if (evt.target == this)
                {
                    m_ScrollView.contentContainer.Focus();
                }
            }
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (Mathf.Approximately(evt.newRect.width, evt.oldRect.width) &&
                Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            Resize(evt.newRect.size, evt.layoutPass);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (!m_ItemHeightIsInline && e.customStyle.TryGetValue(s_ItemHeightProperty, out var height))
            {
                if (Math.Abs(m_FixedItemHeight - height) > float.Epsilon)
                {
                    m_FixedItemHeight = height;
                    RefreshItems();
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            RefreshItems();
        }
    }
}

using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IntegrityTables.Editor
{
    [Serializable]
    public class ScriptableTableAssetEditor : IDisposable
    {
        private MultiColumnListView _listView;
        private VisualElement _root;


        public ScriptableTableAssetEditor(ScriptableDatabase database, ITable scriptableView)
        {
            TargetView = scriptableView;
            TargetView.OnRowModified -= OnRowModified;
            TargetView.OnRowModified += OnRowModified;
        }

        public void Dispose()
        {
            if (TargetView != null)
            {
                TargetView.OnRowModified -= OnRowModified;
            }
        }

        private ITable TargetView { get; set; }

        private void OnRowModified(int index, TableOperation operation)
        {
            switch (operation)
            {
                case TableOperation.Add:
                    RebindList();
                    break;
                case TableOperation.Update:
                    _listView?.RefreshItem(index);
                    break;
                case TableOperation.Remove:
                    RebindList();
                    break;
                case TableOperation.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        public VisualElement CreateGUI()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPeformed;
            Undo.undoRedoPerformed += OnUndoRedoPeformed;

            _root = new VisualElement();
            _listView = CreateListView();
            _root.Add(CreateHeader());
            _root.Add(_listView);
            RebindList();
            return _root;
        }

        private void OnUndoRedoPeformed()
        {
            // need to make sure database has same data
            RebindList();
        }

        private VisualElement CreateHeader()
        {
            var header = new VisualElement {style = {paddingTop = 5, paddingLeft = 5, flexDirection = FlexDirection.Row}};
            return header;
        }

        private MultiColumnListView CreateListView()
        {
            var listView = new MultiColumnListView
            {
                reorderable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                allowAdd = false,
                allowRemove = false,
                showAddRemoveFooter = false,
                sortingMode = ColumnSortingMode.Custom,
                style =
                {
                    flexGrow = 1,
                    marginTop = 8,
                    marginBottom = 8,
                    marginLeft = 0,
                    marginRight = 0
                }
            };

            BuildColumns(listView);

            listView.itemsSource = Enumerable.Range(0, TargetView.Count).ToList();
            return listView;
        }


        private void RebindList()
        {
            _listView.itemsSource = Enumerable.Range(0, TargetView.Count).ToList();
            _listView.RefreshItems();
        }

        private void BuildColumns(MultiColumnListView listView)
        {
            listView.columns.Clear();
            foreach (var fieldInfo in TargetView.Metadata)
            {
                var fieldName = fieldInfo.name;
                var fieldIndex = fieldInfo.index;
                var displayName = ObjectNames.NicifyVariableName(fieldName.Replace("_id", " ID"));

                var column = new Column
                {
                    title = displayName,
                    width = 100,
                    resizable = true,
                    makeCell = () => new VisualElement() {style = {height = 22, flexDirection = FlexDirection.ColumnReverse, borderRightWidth = 1, borderRightColor = new Color(0.2f, 0.2f, 0.2f)}},
                    unbindCell = (cell, rowIndex) => { cell.Unbind(); },
                    bindCell = (cell, rowIndex) =>
                    {
                        cell.Clear();
                        var rowAdapter = TargetView[rowIndex];

                        if (rowAdapter == null)
                        {
                            cell.Add(new Label("Row not found") {style = {paddingLeft = 8, paddingBottom = 4}});
                            return;
                        }

                        cell.Add(CreateCellLabel(rowAdapter, fieldIndex));
                    }
                };
                listView.columns.Add(column);
            }
        }

        private VisualElement CreateCellLabel(RowObjectAdapter rowObjectAdapter, int fieldIndex)
        {
            if (rowObjectAdapter == null) throw new ArgumentNullException(nameof(rowObjectAdapter));
            return new Label(GetNiceLabelText(rowObjectAdapter, fieldIndex)) {style = {paddingLeft = 8, paddingBottom = 4}};
        }

        private string GetNiceLabelText(RowObjectAdapter rowObject, int fieldIndex)
        {
            var value = rowObject[fieldIndex];
            return value switch
            {
                float i => i.ToString("0.###"),
                bool b => b ? "✓" : "✗",
                Object o => o != null ? o.name : "[None]",
                string s => s,
                null => "[None]",
                _ => value.ToString()
            };
        }

        
    }

}
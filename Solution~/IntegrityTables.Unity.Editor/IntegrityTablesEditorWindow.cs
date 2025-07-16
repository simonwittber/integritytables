using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace IntegrityTables.Editor
{
    public class IntegrityTablesEditorWindow : EditorWindow
    {
        // public ScriptableDatabase database;
        public ScriptableTableAssetEditor editor;
        
        private ScrollView tableSelector;
        private VisualElement editorContainer;
        private Dictionary<Type, ITable> tableTypes = new();
        
        public ScriptableDatabase _databaseAsset;
        
        
        private ITable _target;
        public string _targetName;

        private void SetTarget(ITable view, int id=-1)
        {
            _target = view;
            if(editor != null)
            {
                editor.Dispose();
                editorContainer.Clear();
                editor = null;
            }

            if (_target != null)
            {
                editor = new ScriptableTableAssetEditor(_databaseAsset, _target);
                if(editor != null)
                {
                    //_target.UpdateView();
                    var gui = editor.CreateGUI();
                    editorContainer.Add(gui);
                    gui.StretchToParentSize();
                }
            }

            _targetName = _target?.Name;
        }

        private void CreateGUI()
        {
            // create two panes seperated by a splitter
            var root = rootVisualElement;
            root.Clear();
            var split = new TwoPaneSplitView();
            split.viewDataKey = nameof(IntegrityTablesEditorWindow);
            split.orientation = TwoPaneSplitViewOrientation.Horizontal;
            tableSelector = new ScrollView(ScrollViewMode.Vertical);
            editorContainer = new VisualElement() { name = "EditorContainer", style = { flexGrow = 1, flexDirection = FlexDirection.Column}};
            split.Add(tableSelector);
            split.Add(editorContainer);
            root.Add(split);
            if (_targetName != null && _databaseAsset != null)
            {
                var db = _databaseAsset.database;
                var table = db.Tables.FirstOrDefault(i => i.Name == _targetName);
                if (table != null)
                {
                    SetTarget(table);
                }
            }
            UpdateTableSelector();
        }

        private void OnProjectChange()
        {
            UpdateTableSelector();
        }

        private void UpdateTableSelector()
        {
            if (tableSelector == null) return;
            if (_databaseAsset == null)
            {
                tableSelector.Clear();
                return;
            }

            var db = _databaseAsset.database;
            var allTables = db.Tables.ToList();
            var sortedTables = allTables.OrderBy(t => t.Metadata.Group).ThenBy(t => t.Name);
            tableSelector.Clear();
            tableTypes.Clear();
            string group = null;
            foreach (var table in sortedTables)
            {
                tableTypes[table.RowType] = table;
                var button = new Button(() => SetTarget(table))
                {
                    text = ObjectNames.NicifyVariableName(table.Name),
                    tooltip = $"({table.Count} rows)",
                    style =
                    {
                        flexGrow = 1,
                        height = 22
                    }
                };
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                if (group != table.Metadata.Group)
                {
                    group = table.Metadata.Group;
                    var groupLabel = new Label(string.IsNullOrEmpty(group)?"Global":group)
                    {
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleLeft,
                            fontSize = 14,
                            marginTop = 5,
                            marginBottom = 5
                        }
                    };
                    tableSelector.Add(groupLabel);
                }
                tableSelector.Add(button);
            }
        }

        public static void Edit(ScriptableDatabase instance)
        {
            var window = GetWindow<IntegrityTablesEditorWindow>();
            window.SetDatabase(instance);
            window.Show();
        }

        private void SetDatabase(ScriptableDatabase instance)
        {
            _databaseAsset = instance;
            UpdateTableSelector();
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if(obj is ScriptableDatabase st)
            {
                Edit(st);
                return true;
            }
            return false;
        }
        
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            if (editor != null)
            {
                editor = null;
            }
        }

        public void Find(Type refType, int id)
        {
            SetTarget(tableTypes[refType], id);
        }

        public void Find(Type fieldInfoType, string fieldName, int id)
        {
            SetTarget(tableTypes[fieldInfoType]);
        }
    }
}
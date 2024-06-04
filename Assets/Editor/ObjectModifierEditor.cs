using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectModifier : EditorWindow
{
    /// <summary>
    /// ���� ������Ʈ.
    /// </summary>
    private GameObject _objA;
    /// <summary>
    /// ����Ʈ ������Ʈ.
    /// </summary>
    private GameObject _objB;
    /// <summary>
    /// ���� ������Ʈ.
    /// </summary>
    private GameObject _objNew;
    /// <summary>
    /// ����Ʈ ������Ʈ ����.
    /// </summary>
    private GameObject _objOrigin;

    /// <summary>
    /// �� ������ ������Ʈ.
    /// </summary>
    private List<GameObject> _objParts;

    /// <summary>
    /// �� ������ Ʈ������.
    /// </summary>
    private Transform[] _TraChildren;

    /// <summary>
    /// �� ������ ������ �� �迭.
    /// </summary>
    private Vector3[] _vecPartPosition;
    /// <summary>
    /// ��ũ�� ��ġ�� �����ϴ� ����.
    /// </summary>
    private Vector2 _scrollPosition = Vector2.zero;
    /// <summary>
    /// �巡�� ���� ������ �����ϴ� ����.
    /// </summary>
    private Vector2 _dragStartPosition;
    /// <summary>
    /// �巡�� �������� �����ϴ� ����.
    /// </summary>
    private Vector2 _dragOffset;

    /// <summary>
    /// ������ ���� ����.
    /// </summary>
    private int _nCount;
    /// <summary>
    /// ���� �����ߴ���.
    /// </summary>
    private bool _bChanged = false;

    [MenuItem("Tool/ObjectModifier")]
    private static void ShowWindow()
    {
        GetWindow<ObjectModifier>("Modifier");
    }

    private void OnGUI()
    {
        HandleDragging();

        // ���� ������Ʈ ĳ��.
        GUILayout.Label("Original Object:", EditorStyles.boldLabel);
        _objA = EditorGUILayout.ObjectField(_objA, typeof(GameObject), true) as GameObject;

        // ����Ʈ ������Ʈ ĳ��.
        GUILayout.Label("Default Object:", EditorStyles.boldLabel);
        _objB = EditorGUILayout.ObjectField(_objB, typeof(GameObject), true) as GameObject;

        if (_objOrigin == null && _objB != null)
            _objOrigin = GameObject.Instantiate(_objB, Vector3.zero, Quaternion.identity, null) as GameObject;

        // ������ ���� ����.
        _nCount = EditorGUILayout.IntField("Count : ", _nCount);

        // ��ũ�� �� ����.
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height - 100));

        // �� ������ ������Ʈ ĳ��.       
        GUILayout.Label("Body Parts:", EditorStyles.boldLabel);
        if (_objB != null)
        {
            if (_TraChildren == null)
                _TraChildren = _objB.GetComponentsInChildren<Transform>();

            List<GameObject> childObjects = new List<GameObject>();
            for (int i = 0; i < _TraChildren.Length; i++)
            {
                if (_TraChildren[i].name == _objB.name)
                    continue;

                childObjects.Add(_TraChildren[i].gameObject);
            }

            _nCount = childObjects.Count;
            _objParts = childObjects;
        }

        if (_objParts != null)
        {
            if (_vecPartPosition == null)
                _vecPartPosition = new Vector3[_nCount];

            for (int i = 0; i < _vecPartPosition.Length; i++)
            {
                _vecPartPosition[i] = _objParts[i].transform.localPosition;
            }

            int nIndex = -1;
            for (int i = 0; i < _vecPartPosition.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                var newPosition = EditorGUILayout.Vector3Field(_objParts[i].name, _vecPartPosition[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    _vecPartPosition[i] = newPosition;
                    nIndex = i;
                    _bChanged = true;
                }
            }

            if (_bChanged)
            {
                if (nIndex != -1)
                {
                    Calculate(nIndex);
                    _bChanged = false;
                }
            }
        }

        GUILayout.Space(50);
        if (GUILayout.Button("Apply"))
        {
            Modifier();
        }

        GUILayout.EndScrollView();
    }

    private void HandleDragging()
    {
        EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, 20), MouseCursor.MoveArrow);

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            _dragStartPosition = Event.current.mousePosition;
            _dragOffset = Vector2.zero;
            GUI.FocusControl(null);
        }
        else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            Vector2 currentMousePosition = Event.current.mousePosition;
            Vector2 delta = currentMousePosition - _dragStartPosition;
            _dragOffset += delta;
            _dragStartPosition = currentMousePosition;

            // â ��ġ ������Ʈ.
            Rect newPosition = position;
            newPosition.position += delta;
            position = newPosition;

            Event.current.Use();
        }
    }

    private void Calculate(int nIndex)
    {
        if (_objParts == null)
            return;

        Transform tra = _objParts[nIndex].GetComponent<Transform>();

        if (tra != null)
            tra.localPosition = _vecPartPosition[nIndex];
    }

    private void Modifier()
    {
        if (_objA == null || _objB == null)
            return;

        // A ���� ������Ʈ�� �����Ͽ� ���ο� ������Ʈ C ����.
        _objNew = Instantiate(_objA);
        _objNew.name = "_" + _objA.name;

        // B ������Ʈ�� ���� ���Ͽ� ���ο� ������Ʈ C�� ���� ����.        
        AddValues(_objNew);

        // ���ο� ������Ʈ�� ����.
        Save(_objNew);
    }

    private void AddValues(GameObject newObject)
    {
        Transform[] traNewChildren = newObject.GetComponentsInChildren<Transform>();
        Transform[] traObjB = _objB.GetComponentsInChildren<Transform>();
        foreach (var child in traNewChildren)
        {
            foreach (var v in traObjB)
            {
                if (child.name == v.name)
                {
                    if (child.transform.localPosition == v.transform.localPosition)
                        continue;

                    child.transform.localPosition += v.transform.localPosition;
                }
            }
        }
    }

    private void Save(GameObject newObject)
    {
        string localPath = "Assets/" + newObject.name + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        PrefabUtility.SaveAsPrefabAsset(newObject, localPath);
        Clear();
        Close();
    }

    private void Clear()
    {
        Transform[] traNewChildren = _objB.GetComponentsInChildren<Transform>();
        Transform[] traObjB = _objOrigin.GetComponentsInChildren<Transform>();
        foreach (var child in traNewChildren)
        {
            foreach (var v in traObjB)
            {
                if (child.name == v.name)
                {
                    if (child.transform.localPosition == v.transform.localPosition)
                        continue;

                    child.transform.localPosition = v.transform.localPosition;
                }
            }
        }

        DestroyImmediate(_objNew);
        DestroyImmediate(_objOrigin);

        _TraChildren = null;
        _vecPartPosition = null;
    }
}
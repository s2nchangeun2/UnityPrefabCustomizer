using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectModifier : EditorWindow
{
    /// <summary>
    /// 원본 오브젝트.
    /// </summary>
    private GameObject _objA;
    /// <summary>
    /// 디폴트 오브젝트.
    /// </summary>
    private GameObject _objB;
    /// <summary>
    /// 생성 오브젝트.
    /// </summary>
    private GameObject _objNew;
    /// <summary>
    /// 디폴트 오브젝트 복제.
    /// </summary>
    private GameObject _objOrigin;

    /// <summary>
    /// 각 부위별 오브젝트.
    /// </summary>
    private List<GameObject> _objParts;

    /// <summary>
    /// 각 부위별 트랜스폼.
    /// </summary>
    private Transform[] _TraChildren;

    /// <summary>
    /// 각 부위의 포지션 값 배열.
    /// </summary>
    private Vector3[] _vecPartPosition;
    /// <summary>
    /// 스크롤 위치를 저장하는 변수.
    /// </summary>
    private Vector2 _scrollPosition = Vector2.zero;
    /// <summary>
    /// 드래그 시작 지점을 저장하는 변수.
    /// </summary>
    private Vector2 _dragStartPosition;
    /// <summary>
    /// 드래그 오프셋을 저장하는 변수.
    /// </summary>
    private Vector2 _dragOffset;

    /// <summary>
    /// 수정할 부위 개수.
    /// </summary>
    private int _nCount;
    /// <summary>
    /// 값을 수정했는지.
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

        // 원본 오브젝트 캐싱.
        GUILayout.Label("Original Object:", EditorStyles.boldLabel);
        _objA = EditorGUILayout.ObjectField(_objA, typeof(GameObject), true) as GameObject;

        // 디폴트 오브젝트 캐싱.
        GUILayout.Label("Default Object:", EditorStyles.boldLabel);
        _objB = EditorGUILayout.ObjectField(_objB, typeof(GameObject), true) as GameObject;

        if (_objOrigin == null && _objB != null)
            _objOrigin = GameObject.Instantiate(_objB, Vector3.zero, Quaternion.identity, null) as GameObject;

        // 수정할 부위 개수.
        _nCount = EditorGUILayout.IntField("Count : ", _nCount);

        // 스크롤 뷰 시작.
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height - 100));

        // 각 부위별 오브젝트 캐싱.       
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

            // 창 위치 업데이트.
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

        // A 원본 오브젝트를 복제하여 새로운 오브젝트 C 생성.
        _objNew = Instantiate(_objA);
        _objNew.name = "_" + _objA.name;

        // B 오브젝트의 값을 더하여 새로운 오브젝트 C의 값을 수정.        
        AddValues(_objNew);

        // 새로운 오브젝트를 저장.
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
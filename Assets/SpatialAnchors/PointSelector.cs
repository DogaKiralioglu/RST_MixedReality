using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using static TMPro.TMP_Compatibility;

public class PointSelector : MonoBehaviour
{
    public static PointSelector Instance;

    [SerializeField]
    private GameObject _refAnchorPrefab;

    [SerializeField]
    private GameObject _refPreview;

    [SerializeField]
    private Transform _refTransform;

    [SerializeField]
    private GameObject _prefabToPlace;

    [SerializeField]
    private Transform _prefabBasePoint;

    private List<OVRSpatialAnchor> _refAnchors = new();
    private List<OVRSpatialAnchor> _refAnchorInstances = new(); //active instances
    private HashSet<Guid> _anchorUuids = new(); //simulated external location, like PlayerPrefs
    private List<GameObject> _instantiatedRefPrefabs = new List<GameObject>(); //anchor prefabs
    private List<Vector3> _anchorPositions = new(); // Store anchor positions

    private bool isPlacingPrefab = false;

    // Start is called before the first frame update
    void Start()
    {
        isPlacingPrefab = false;
        //Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlacingPrefab && OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // Create a green capsule
        {
            // Create a reference anchor
            var go = Instantiate(_refAnchorPrefab, _refTransform.position, _refTransform.rotation); // Anchor A
            SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: true);
        }

        //else if (OVRInput.GetDown(OVRInput.Button.Three)) // x button
        //{
        //    // Destroy all anchors from the scene, but don't erase them from storage
        //    foreach (var anchor in _refAnchors)
        //    {
        //        Destroy(anchor.gameObject);
        //    }

        //    // Destroy all instantiated anchor prefabs
        //    foreach (GameObject prefabInstance in _instantiatedRefPrefabs)
        //    {
        //        Destroy(prefabInstance);
        //    }
        //    _instantiatedRefPrefabs.Clear();

        //    // Clear the list of running anchors
        //    _refAnchors.Clear();
        //}

        // Check for prefab placement activation via button click

        if (isPlacingPrefab && _anchorPositions.Count > 0)
        {
            PlaceRefPrefab();
            isPlacingPrefab = false; // Deactivate placement after one prefab is placed
        }
    }

    //public void ActivatePrefabPlacement()
    //{

    //    isPlacingPrefab = true;
    //}

    private async void SetupAnchorAsync(OVRSpatialAnchor anchor, bool saveAnchor)
    {
        while (!anchor.Created && !anchor.Localized)
        {
            await Task.Yield();
        }

        _refAnchorInstances.Add(anchor);
        _anchorPositions.Add(anchor.transform.position);

        if (saveAnchor && (await anchor.SaveAnchorAsync()).Success)
        {
            _anchorUuids.Add(anchor.Uuid);
            _refAnchors.Add(anchor);
        }

        if (_refAnchorInstances.Count == 1)
        {
            PlaceRefPrefab();
        }

    }
    private void PlaceRefPrefab()
    {
        if (_anchorPositions.Count < 1)
        {
            Debug.LogWarning("Not enough anchors to place the prefab. You need 1 anchor.");
            return;
        }

        Vector3 anchor1 = _anchorPositions[0];

        Vector3 objectPoint1 = _prefabBasePoint.position;

        Vector3 offset = anchor1 - objectPoint1;

        // Instantiate the prefab at the new position with the calculated rotation
        GameObject prefabInstance = Instantiate(_prefabToPlace, _prefabToPlace.transform.position + offset, _prefabToPlace.transform.rotation);

        // Add the instantiated prefab to the list
        _instantiatedRefPrefabs.Add(prefabInstance);

        // Clear anchor positions for the next set of anchors
        _anchorPositions.Clear();

    }   

    public void OnClick()
    {
        isPlacingPrefab = true;
    }
  }

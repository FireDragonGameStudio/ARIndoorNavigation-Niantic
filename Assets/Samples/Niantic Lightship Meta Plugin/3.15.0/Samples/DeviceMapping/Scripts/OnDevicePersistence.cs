// Copyright 2022-2025 Niantic.

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.Lightship.MetaQuest.InternalSamples {
    public class OnDevicePersistence : MonoBehaviour {
        [Header("Managers")]
        [SerializeField]
        private Mapper _mapper;

        [SerializeField]
        private Tracker _tracker;

        [Header("UX - Status")]
        [SerializeField]
        private Text _statusText;

        [Header("UX - Create/Load")]
        [SerializeField]
        private GameObject _createLoadPanel;

        [SerializeField]
        private Button _createMapButton;

        [SerializeField]
        private Button _loadMapButton;

        [Header("UX - Scan Map")]
        [SerializeField]
        private GameObject _scanMapPanel;

        [SerializeField]
        private Button _startScanning;

        [SerializeField]
        private Button _exitScanMapButton;

        [Header("UX - Scanning Animation")]
        [SerializeField]
        private GameObject _scanningAnimationPanel;

        [Header("UX - In Game")]
        [SerializeField]
        private GameObject _inGamePanel;

        [SerializeField]
        private Button _placeCubeButton;

        [SerializeField]
        private Button _saveCubesButton;

        [SerializeField]
        private Button _deleteCubesButton;

        [SerializeField]
        private Button _exitInGameButton;

        [Header("Anchor - References")]
        [SerializeField] private GameObject _anchorCubePrefab;
        [SerializeField] private Text _debugText;

        //files to save to
        public static string k_mapFileName = "ADHocMapFile";
        public static string k_objectsFileName = "ADHocObjectsFile";

        /// <summary>
        /// Set up to main menu on start
        /// </summary>
        void Start() {
            SetUp_CreateMenu();
        }

        /// <summary>
        /// Exit to main menu
        /// </summary>
        private void Exit() {
            _statusText.text = "";

            if (_tracker.Anchor) {
                for (int i = 0; i < _tracker.Anchor.transform.childCount; i++)
                    Destroy(_tracker.Anchor.transform.GetChild(i).gameObject);
            }

            //make sure all menu are destroyed
            Teardown_InGameMenu();
            Teardown_LocalizeMenu();
            Teardown_ScanningMenu();
            Teardown_CreateMenu();

            //tracking if running needs to be stopped on exit.
            StartCoroutine(ClearTrackingAndMappingState());

            //go back to the main menu
            SetUp_CreateMenu();
        }

        private IEnumerator ClearTrackingAndMappingState() {
            // Both ARPersistentAnchorManager and
            // need to be diabled before calling ClearDeviceMap()

            _mapper.ClearAllState();
            yield return null;

            _tracker.ClearAllState();
            yield return null;
        }

        /// <summary>
        /// Create Map / Load map functions
        /// </summary>
        private bool CheckForSavedMap(string MapFileName) {
            var path = Path.Combine(Application.persistentDataPath, MapFileName);
            if (System.IO.File.Exists(path)) {
                return true;
            }

            return false;
        }

        private void SetUp_CreateMenu() {
            //hide other menus
            Teardown_InGameMenu();
            Teardown_ScanningMenu();
            Teardown_LocalizeMenu();

            _createLoadPanel.SetActive(true);

            _createMapButton.onClick.AddListener(SetUp_ScanMenu);
            _loadMapButton.onClick.AddListener(SetUp_LocalizeMenu);

            _createMapButton.interactable = true;

            //if there is a saved map enable the load button.
            if (CheckForSavedMap(k_mapFileName)) {
                _loadMapButton.interactable = true;
            } else {
                _loadMapButton.interactable = false;
            }
        }

        private void Teardown_CreateMenu() {
            _createLoadPanel.gameObject.SetActive(false);
            _createMapButton.onClick.RemoveAllListeners();
            _loadMapButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Scan Map functions
        /// </summary>
        private void SetUp_ScanMenu() {
            Teardown_CreateMenu();
            _scanMapPanel.SetActive(true);
            _startScanning.onClick.AddListener(StartScanning);
            _exitScanMapButton.onClick.AddListener(Exit);

            _startScanning.interactable = true;
            _exitScanMapButton.interactable = true;
        }

        private void Teardown_ScanningMenu() {
            _startScanning.onClick.RemoveAllListeners();
            _exitScanMapButton.onClick.RemoveAllListeners();
            _scanMapPanel.gameObject.SetActive(false);
            _mapper._onMappingComplete -= MappingComplete;
            _mapper.StopMapping();
        }

        private void StartScanning() {
            _startScanning.interactable = false;
            _statusText.text = "Look Around to create map";
            _mapper._onMappingComplete += MappingComplete;
            float time = 5.0f;
            _mapper.RunMappingFor(time);

            if (_scanningAnimationPanel) {
                _scanningAnimationPanel.SetActive(true);
            }
        }

        private void MappingComplete(bool success) {
            if (success) {
                //clear out any cubes
                DeleteCubes();
                if (_scanningAnimationPanel) {
                    _scanningAnimationPanel.SetActive(false);
                }

                //jump to localizing.
                SetUp_LocalizeMenu();
            } else {
                //failed to make a map try again.
                _startScanning.interactable = true;
                _statusText.text = "Map Creation Failed Try Again";
            }

            _mapper._onMappingComplete -= MappingComplete;
        }

        /// <summary>
        /// Localization to Map functions
        /// </summary>
        private void SetUp_LocalizeMenu() {
            Teardown_CreateMenu();
            Teardown_ScanningMenu();
            //go to tracking and localise to the map.
            _statusText.text = "Move Phone around to localize to map";
            _tracker._tracking += Localized;
            _tracker.StartTracking();

            if (_scanningAnimationPanel) {
                _scanningAnimationPanel.SetActive(true);
            }
        }

        private void Teardown_LocalizeMenu() {
            _tracker._tracking -= Localized;
            if (_scanningAnimationPanel) {
                _scanningAnimationPanel.SetActive(false);
            }
        }

        private void Localized(bool localized) {
            //once we are localised we can open the main menu.
            if (localized == true) {
                _statusText.text = "";
                _tracker._tracking -= Localized;
                SetUp_InGameMenu();
                LoadCubes();
                if (_scanningAnimationPanel) {
                    _scanningAnimationPanel.SetActive(false);
                }
            } else {
                //failed exit out.
                Exit();
            }
        }

        /// <summary>
        /// In game functions
        /// </summary>
        private void SetUp_InGameMenu() {
            Teardown_LocalizeMenu();
            Teardown_ScanningMenu();

            _inGamePanel.SetActive(true);
            _placeCubeButton.onClick.AddListener(PlaceCube);
            _saveCubesButton.onClick.AddListener(SaveCubes);
            _deleteCubesButton.onClick.AddListener(DeleteCubes);
            _exitInGameButton.onClick.AddListener(Exit);

            _placeCubeButton.interactable = true;
            _saveCubesButton.interactable = true;
            _exitInGameButton.interactable = true;
        }

        private void Teardown_InGameMenu() {
            _placeCubeButton.onClick.RemoveAllListeners();
            _saveCubesButton.onClick.RemoveAllListeners();
            _exitInGameButton.onClick.RemoveAllListeners();
            _deleteCubesButton.onClick.RemoveAllListeners();
            _inGamePanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Manging the cude placement/storage and anchoring to map function
        /// </summary>
        private void CreateAndPlaceCube(Vector3 localPos) {

            // init and add it under the anchor on our map.
            var go = GameObject.Instantiate(_anchorCubePrefab, localPos, Quaternion.identity,
                new InstantiateParameters() {
                    parent = _tracker.Anchor,
                    worldSpace = false
                }
            );

            _debugText.text = "Placed at anchor: " + _tracker.Anchor.position + " - " + go.transform.localPosition;
        }

        private void PlaceCube() {
            //place a cube 1m in front of the camera.
            var pos = Camera.main.transform.position + (Camera.main.transform.forward * 1.0f);
            CreateAndPlaceCube(_tracker.GetAnchorRelativePosition(pos));
        }

        private void SaveCubes() {

            var fileName = OnDevicePersistence.k_objectsFileName;
            var path = Path.Combine(Application.persistentDataPath, fileName);

            File.Delete(path);

            using (StreamWriter sw = File.AppendText(path)) {
                if (_tracker.Anchor) {
                    for (int i = 0; i < _tracker.Anchor.transform.childCount; i++) {
                        sw.WriteLine(_tracker.Anchor.transform.GetChild(i).gameObject.transform.localPosition);
                        _debugText.text = "Saved at anchor: " + _tracker.Anchor.position + " - " + _tracker.Anchor.transform.GetChild(i).gameObject.transform.localPosition;
                    }
                }
            }
        }

        private void LoadCubes() {
            if (_tracker.Anchor) {
                for (int i = 0; i < _tracker.Anchor.transform.childCount; i++)
                    Destroy(_tracker.Anchor.transform.GetChild(i).gameObject);
            }

            var fileName = OnDevicePersistence.k_objectsFileName;
            var path = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(path)) {
                using (StreamReader sr = new StreamReader(path)) {
                    while (sr.Peek() >= 0) {
                        var pos = sr.ReadLine();
                        var split1 = pos.Split("(");
                        var split2 = split1[1].Split(")");
                        var parts = split2[0].Split(",");
                        Vector3 localPos = new Vector3(
                            System.Convert.ToSingle(parts[0]),
                            System.Convert.ToSingle(parts[1]),
                            System.Convert.ToSingle(parts[2])
                        );

                        CreateAndPlaceCube(localPos);
                    }
                }
            }
        }

        private void DeleteCubes() {
            //delete from the file.
            var fileName = OnDevicePersistence.k_objectsFileName;
            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.Delete(path);

            //delete from in game.
            if (_tracker.Anchor) {
                for (int i = 0; i < _tracker.Anchor.transform.childCount; i++)
                    Destroy(_tracker.Anchor.transform.GetChild(i).gameObject);
            }
        }
    }
}

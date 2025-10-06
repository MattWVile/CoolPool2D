using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;



public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public Data Data { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Update()
    {
        // For testing purposes: Press Ctrl+S to create a new save file and overwrite the existing one.
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.P)) {
            CreateNewSaveFile();
            Debug.Log("New save file created.");
        }
    }

    private void Start()
    {
        LoadDataFromFileSafely();
    }

    private void LoadDataFromFileSafely()
    {
        Data = SaveFileUtils.LoadDataFromFile();
        
    }

    private void CreateNewSaveFile()
    {
        Data = new Data() {
            MapData = new MapData() {
                GeneratedMap = null,
                CurrentNode = null
            }
        };
        SaveData();
        
    }
    public void SaveData() {
        SaveFileUtils.SaveDataToFile(Data);
    }
}
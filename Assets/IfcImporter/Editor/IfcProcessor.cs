﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace IfcToolkit {

/// <summary>Class hooking into the Unity editor's import pipeline to detect when an ifc-file needs importing, then imports it.</summary>
public class IfcProcessor : AssetPostprocessor
{
    ///List of ifc files being imported
    private static List<string> assetPaths = new List<string>();
    private static DateTime? assetCreateTime = null;
    
    ///<summary>Generates the .dae and .xml files for newly detected .ifc files.</summary>
    ///<remarks>OnPreprocessAsset() is called before a file is imported into the Unity editor.</remarks>
    void OnPreprocessAsset()
    {
        if (assetPath.EndsWith(".ifc"))
        {
            //UnityEngine.Debug.Log("Preprocessing " + assetPath);
            bool editor = true;
            IfcImporter.ProcessIfc(assetPath, IfcSettingsWindow.options, editor);
            
            //Import the newly created files
            string outputFile = "Assets/IfcImporter/Resources/" + System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.ImportAsset(outputFile + "_dae.dae", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(outputFile + "_xml.xml", ImportAssetOptions.ForceUpdate);

            //Add the ifc file to list of ifc files being imported
            assetPaths.Add(assetPath);
            //Avoid adding multiple AttemptFinishImports to EditorApplication.update
            if (assetCreateTime is null) {
                //Keep trying to finish the job until we can load the dae file
                EditorApplication.update += AttemptFinishImport;
            }
            //Keep track of the time so we don't get stuck trying to import something forever
            assetCreateTime = System.DateTime.Now;
        }
    }

    ///<summary>Creates prefabs for newly detected .ifc files.</summary>
    ///<remarks>Will keep retrying until the dae file exists.</remarks>
    private void AttemptFinishImport()
    {
        foreach (string assetPath in new List<string>(assetPaths)) {
            string resourceName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (Resources.Load(resourceName + "_dae") != null) {
                assetPaths.Remove(assetPath);

                GameObject gameObject = IfcImporter.LoadDae(assetPath);

                string xml_path = "Assets/IfcImporter/Resources/" +resourceName + "_xml.xml";
                IfcXmlParser.parseXmlFile(xml_path, gameObject, IfcSettingsWindow.options);

                //Store filename in Ifc File component
                IfcFile ifcFile = gameObject.AddComponent<IfcFile>() as IfcFile;
                ifcFile.ifcFileName = resourceName;
                ifcFile.ifcFilePath = assetPath;
                ifcFile.geometryFileFormat = "DAE";
                
                //Move the model to origin if keepOriginalPositionEnabled is false
                if (!IfcSettingsWindow.options.keepOriginalPositionEnabled) {
                    IfcImporter.MoveToOrigin(gameObject);
                }
                if (!IfcSettingsWindow.options.keepOriginalPositionForPartsEnabled) {
                    IfcImporter.MovePartsToOrigin(gameObject);
                }

                PrefabSaver.savePrefab(gameObject, assetPath);

                if (assetPaths.Count == 0) {
                    EditorApplication.update -= AttemptFinishImport;
                    assetCreateTime = null;
                    return;
                }
            }
        }

        //Abort if more than ten seconds have passed since the dae should've been created
        if (assetCreateTime == null || System.DateTime.Now - assetCreateTime > new TimeSpan(0,0,10)) {
            Debug.Log("Loading the .dae file generated by IfcConvert failed.");
            EditorApplication.update -= AttemptFinishImport;
            assetCreateTime = null;
            assetPaths = new List<string>();
        }
    }
}

}

using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using UnityEngine;

[System.Serializable]
public struct Settings
{
    public float m_fBossVolume;
    public float m_fPlayerVolume;
    public float m_fGrappleVolume;
    public float m_fWindVolume;
}

public class SettingsIO
{
    private Settings m_data;
    private string m_path = "";

    public SettingsIO()
    {
        m_data = new Settings();
        m_path = Application.dataPath + "/Settings.dat";
    }

    public Settings GetData()
    {
        return m_data;
    }

    public void SetData(Settings data)
    {
        m_data = data;
    }

    public void WriteFile()
    {
        try
        {
            // Create file handle and open file...
            FileStream file = new FileStream(m_path, FileMode.OpenOrCreate);

            // Create formatter.
            BinaryFormatter formatter = new BinaryFormatter();

            // Write file.
            formatter.Serialize(file, m_data);
            
            // Close file handle.
            file.Close();
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void ReadFile()
    {
        try
        {
            // Open file...
            FileStream file = new FileStream(m_path, FileMode.Open);

            // Create formatter.
            BinaryFormatter formatter = new BinaryFormatter();

            // Read file.
            m_data = (Settings)formatter.Deserialize(file);

            // Close file handle.
            file.Close();
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
}

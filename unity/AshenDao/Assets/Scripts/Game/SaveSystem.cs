using System;
using System.IO;
using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    [Serializable]
    public class UnitySave
    {
        public SaveData Sim;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
    }

    public static class SaveSystem
    {
        private const string FileName = "ashen-dao-save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave => File.Exists(SavePath);

        public static void Save(GameRoot root)
        {
            var save = new UnitySave
            {
                Sim = root.Sim.ExportSave(),
                X = root.Player.transform.position.x,
                Y = root.Player.transform.position.y,
                Z = root.Player.transform.position.z,
                Yaw = root.Player.transform.eulerAngles.y
            };
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            Debug.Log("Saved to " + SavePath);
        }

        public static bool Load(GameRoot root)
        {
            if (!HasSave) return false;
            try
            {
                var json = File.ReadAllText(SavePath);
                var save = JsonUtility.FromJson<UnitySave>(json);
                if (save == null || save.Sim == null) return false;
                root.Sim.ImportSave(save.Sim);
                var controller = root.Player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                root.Player.transform.position = new Vector3(save.X, save.Y, save.Z);
                root.Player.transform.rotation = Quaternion.Euler(0f, save.Yaw, 0f);
                if (controller != null) controller.enabled = true;
                root.CameraRig.Snap();
                if (root.Phone != null) root.Phone.RefreshAll();
                if (root.Hud != null) root.Hud.Refresh();
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Load failed: " + error.Message);
                return false;
            }
        }

        public static void TryAutoLoad(GameRoot root)
        {
            if (GameRoot.SkipAutoLoad) return;
            if (!HasSave) return;
            if (Load(root)) root.Sim.Notify("已读取上次的存档。", NoticeTone.Info);
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}

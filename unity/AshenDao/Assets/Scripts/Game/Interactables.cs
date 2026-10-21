using System.Collections.Generic;
using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    public abstract class InteractableBase : MonoBehaviour
    {
        public static readonly List<InteractableBase> All = new List<InteractableBase>();

        public abstract string Prompt { get; }
        public abstract void Interact(PlayerController player);

        protected virtual void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
        }

        public static InteractableBase FindNearest(Vector3 position, float range)
        {
            InteractableBase best = null;
            var bestDistance = range;
            for (var i = All.Count - 1; i >= 0; i--)
            {
                var candidate = All[i];
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                var distance = Vector3.Distance(position, candidate.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }
    }

    public class ContainerInteractable : InteractableBase
    {
        public string Label = "木箱";
        public List<string> ItemIds = new List<string>();
        public List<int> Counts = new List<int>();
        public bool Opened;
        public Transform Lid;

        public override string Prompt => Opened ? Label + " · 已搜空" : "搜索 " + Label;

        public override void Interact(PlayerController player)
        {
            if (Opened)
            {
                GameRoot.Instance.Sim.Notify(Label + " 已经空了。", NoticeTone.Info);
                return;
            }

            var sim = GameRoot.Instance.Sim;
            var remainingIds = new List<string>();
            var remainingCounts = new List<int>();
            for (var i = 0; i < ItemIds.Count; i++)
            {
                var left = sim.AddItem(ItemIds[i], Counts[i]);
                if (left > 0)
                {
                    remainingIds.Add(ItemIds[i]);
                    remainingCounts.Add(left);
                }
            }

            ItemIds = remainingIds;
            Counts = remainingCounts;
            Opened = ItemIds.Count == 0;
            if (Lid != null) Lid.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            sim.Notify(Opened ? "搜刮完 " + Label + "。" : "背包空间不足，还剩一些。",
                Opened ? NoticeTone.Good : NoticeTone.Warn);
        }
    }

    public class HerbInteractable : InteractableBase
    {
        public override string Prompt => "采集灵草";

        public override void Interact(PlayerController player)
        {
            var sim = GameRoot.Instance.Sim;
            var left = sim.AddItem("spirit_herb", 1);
            if (left > 0)
            {
                sim.Notify("背包已满。", NoticeTone.Warn);
                return;
            }
            sim.Notify("采得灵草 ×1", NoticeTone.Good);
            gameObject.SetActive(false);
        }
    }

    public class SpiritVein : MonoBehaviour
    {
        public string VeinName = "古井灵脉";
        public float Radius = 4f;
        public float Strength = 1.6f;
        public Light Glow;
        public Transform Aura;

        public bool Contains(Vector3 position)
        {
            return Vector3.Distance(transform.position, position) <= Radius;
        }
    }
}

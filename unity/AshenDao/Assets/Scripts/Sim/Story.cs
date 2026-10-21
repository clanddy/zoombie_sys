using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    public enum NpcOrder
    {
        Idle,
        Follow,
        Wait,
        Safehouse
    }

    [Serializable]
    public class NpcState
    {
        public string Id;
        public bool Following;
        public bool AtSafehouse;
        public int Trust;
        public bool Alive = true;
        public string Order = "idle";
        public string LastNode;

        public NpcOrder CurrentOrder
        {
            get
            {
                switch (Order)
                {
                    case "follow": return NpcOrder.Follow;
                    case "wait": return NpcOrder.Wait;
                    case "safehouse": return NpcOrder.Safehouse;
                    default: return NpcOrder.Idle;
                }
            }
        }
    }

    public class DialogueSession
    {
        public string NpcId;
        public string Speaker;
        public string NodeId;
        public string Text;
        public List<DialogueChoiceDef> Choices = new List<DialogueChoiceDef>();
        public bool Done;
    }

    public class StorySystem
    {
        private readonly WorldSim sim;
        private readonly Dictionary<string, NpcState> states = new Dictionary<string, NpcState>();
        private readonly HashSet<string> flags = new HashSet<string>();

        public string PendingTrade { get; private set; }

        public StorySystem(WorldSim world)
        {
            sim = world;
            foreach (var npc in sim.Content.Npcs)
            {
                states[npc.Id] = new NpcState
                {
                    Id = npc.Id,
                    AtSafehouse = npc.AtSafehouse,
                    Order = npc.AtSafehouse ? "safehouse" : "idle",
                    Alive = true
                };
            }
        }

        public NpcState State(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!states.TryGetValue(id, out var state))
            {
                state = new NpcState { Id = id };
                states[id] = state;
            }
            return state;
        }

        public IEnumerable<NpcState> AllStates => states.Values;
        public IEnumerable<string> Flags => flags;

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
            sim.RecordEvent(GameEventType.Story, "flag." + flag, "故事标记：" + flag, 1);
            sim.Quests.NotifyFlag(flag);
        }

        public DialogueSession Start(string npcId)
        {
            var def = sim.Content.Npc(npcId);
            if (def == null) return null;
            var dialogue = sim.Content.Dialogue(def.DialogueId);
            if (dialogue == null) return null;

            var state = State(npcId);
            var startNode = ChooseStartNode(npcId, dialogue, state);
            var session = new DialogueSession
            {
                NpcId = npcId,
                Speaker = def.Name
            };
            var node = FindNode(dialogue, startNode);
            Enter(dialogue, session, node);
            sim.Stats.Dialogues++;
            sim.RecordEvent(GameEventType.Dialogue, "talk." + npcId, "与 " + def.Name + " 交谈", 1);
            return session;
        }

        private string ChooseStartNode(string npcId, DialogueDef dialogue, NpcState state)
        {
            if (state.Following && FindNode(dialogue, "wait") != null) return "wait";
            if (npcId == "ahe")
            {
                if (sim.Quests.IsCompleted("ahe_escort") && HasNode(dialogue, "done")) return "done";
                if (sim.Quests.IsCompleted("first_night") && HasFlag("met_ahe") && HasNode(dialogue, "follow"))
                    return "follow";
            }
            return string.IsNullOrEmpty(dialogue.StartNode) ? "start" : dialogue.StartNode;
        }

        private static bool HasNode(DialogueDef dialogue, string id)
        {
            return FindNode(dialogue, id) != null;
        }

        private static DialogueNodeDef FindNode(DialogueDef dialogue, string id)
        {
            if (dialogue == null || string.IsNullOrEmpty(id)) return null;
            foreach (var node in dialogue.Nodes)
            {
                if (node.Id == id) return node;
            }
            return dialogue.Nodes.Count > 0 ? dialogue.Nodes[0] : null;
        }

        private void Enter(DialogueDef dialogue, DialogueSession session, DialogueNodeDef node)
        {
            if (node == null)
            {
                session.Done = true;
                return;
            }
            session.NodeId = node.Id;
            session.Speaker = string.IsNullOrEmpty(node.Speaker) ? session.Speaker : node.Speaker;
            session.Text = node.Text;
            session.Choices.Clear();
            if (!string.IsNullOrEmpty(node.Action))
            {
                Apply(node.Action);
            }
            foreach (var choice in node.Choices)
            {
                if (!string.IsNullOrEmpty(choice.RequiresFlag) && !HasFlag(choice.RequiresFlag)) continue;
                if (!string.IsNullOrEmpty(choice.RequiresItem) && sim.CountItem(choice.RequiresItem) <= 0) continue;
                session.Choices.Add(choice);
            }
            State(session.NpcId).LastNode = node.Id;
        }

        public DialogueSession Choose(DialogueSession session, int index)
        {
            if (session == null || session.Done) return session;
            if (index < 0 || index >= session.Choices.Count) return session;
            var choice = session.Choices[index];
            if (!string.IsNullOrEmpty(choice.Action)) Apply(choice.Action);
            var dialogue = sim.Content.Dialogue(sim.Content.Npc(session.NpcId).DialogueId);
            if (!string.IsNullOrEmpty(choice.Next))
            {
                Enter(dialogue, session, FindNode(dialogue, choice.Next));
            }
            else
            {
                session.Done = true;
            }
            return session;
        }

        public void Apply(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            var parts = action.Split('|');
            foreach (var part in parts)
            {
                var tokens = part.Split(':');
                var verb = tokens[0];
                switch (verb)
                {
                    case "flag":
                        if (tokens.Length > 1) SetFlag(tokens[1]);
                        break;
                    case "quest":
                        if (tokens.Length > 1) sim.AcceptQuest(tokens[1]);
                        break;
                    case "follow":
                        var followState = State("ahe");
                        followState.Following = true;
                        followState.Order = "follow";
                        sim.RecordEvent(GameEventType.Escort, "follow.ahe", "阿禾开始跟随", 1);
                        sim.Notify("阿禾跟上了你。", NoticeTone.Good);
                        break;
                    case "wait":
                        var waitState = State("ahe");
                        waitState.Following = false;
                        waitState.Order = "wait";
                        sim.RecordEvent(GameEventType.Escort, "wait.ahe", "阿禾留在原地", 1);
                        sim.Notify("阿禾留在原地等你。", NoticeTone.Info);
                        break;
                    case "trade":
                        if (tokens.Length > 1) PendingTrade = tokens[1];
                        break;
                    case "trust":
                        if (tokens.Length > 1) State("ahe").Trust += ParseInt(tokens[1]);
                        break;
                    case "give":
                        if (tokens.Length > 2) sim.AddItem(tokens[1], ParseInt(tokens[2]), false);
                        break;
                    case "end":
                        break;
                }
            }
        }

        public string ConsumePendingTrade()
        {
            var trade = PendingTrade;
            PendingTrade = null;
            return trade;
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        public void Restore(NpcState state)
        {
            if (state == null || string.IsNullOrEmpty(state.Id)) return;
            states[state.Id] = state;
        }

        public void RestoreFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag)) flags.Add(flag);
        }
    }
}

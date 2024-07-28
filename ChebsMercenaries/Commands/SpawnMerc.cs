using ChebsMercenaries.Minions;
using ChebsValheimLibrary.Minions;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsMercenaries.Commands
{
    public class SpawnMerc : ConsoleCommand
    {
        public override string Name => "chebgonaz_spawnmerc";

        public override string Help => "Requires admin. Spawn a mercenary. Case insensitive.\n" +
                                       $"Usage: {Name} [TYPE={TypesAsString()}] [ARMOR={ArmorsAsString()}]\n" +
                                       $"eg. {Name} WarriorTier1 Bronze";

        private static string TypesAsString()
        {
            var values = Enum.GetValues(typeof(MercenaryMinion.MercenaryType)).Cast<MercenaryMinion.MercenaryType>();
            return string.Join("|", values);
        }
        
        private static string ArmorsAsString()
        {
            var values = Enum.GetValues(typeof(ChebGonazMinion.ArmorType)).Cast<ChebGonazMinion.ArmorType>();
            return string.Join("|", values);
        }

        private static Dictionary<string, MercenaryMinion.MercenaryType> _mercTypeLookup;
        private static Dictionary<string, ChebGonazMinion.ArmorType> _armorTypeLookup;

        public override void Run(string[] args)
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                Console.instance.Print("Only admins can run this command.");
                return;
            }

            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }

            if (_mercTypeLookup == null)
            {
                _mercTypeLookup = new Dictionary<string, MercenaryMinion.MercenaryType>();
                foreach (MercenaryMinion.MercenaryType mercType in Enum.GetValues(typeof(MercenaryMinion.MercenaryType)))
                {
                    var key = mercType.ToString().ToLower();
                    _mercTypeLookup.Add(key, mercType);
                }
            }

            if (_armorTypeLookup == null)
            {
                _armorTypeLookup = new Dictionary<string, ChebGonazMinion.ArmorType>();
                foreach (ChebGonazMinion.ArmorType armorType in Enum.GetValues(typeof(ChebGonazMinion.ArmorType)))
                {
                    var key = armorType.ToString().ToLower();
                    _armorTypeLookup.Add(key, armorType);
                }
            }
            
            var chosenMerc = MercenaryMinion.MercenaryType.None;
            if (!_mercTypeLookup.TryGetValue(args[0].ToLower(), out chosenMerc))
            {
                Console.instance.Print($"Invalid type: {args[0]}. Valid options: {TypesAsString()}");
                return;
            }

            var chosenArmor = ChebGonazMinion.ArmorType.None;
            if (args.Length >= 2)
            {
                if (!_armorTypeLookup.TryGetValue(args[1].ToLower(), out chosenArmor))
                {
                    Console.instance.Print($"Invalid armor: {args[1]}. Valid options: {ArmorsAsString()}");
                    return;
                }
            }

            var skinColors = new List<string>() {"#F7DC6F", "#935116", "#AFABAB", "#FF5733", "#1C2833"}
                .Select(str => str.Trim()).ToList().Select(html =>
                ColorUtility.TryParseHtmlString(html, out Color color)
                    ? Utils.ColorToVec3(color)
                    : Vector3.zero).ToList();
            
            var hairColors = new List<string>() {"#F7DC6F", "#935116", "#AFABAB", "#FF5733", "#1C2833"}
                .Select(str => str.Trim()).ToList().Select(html =>
                    ColorUtility.TryParseHtmlString(html, out Color color)
                        ? Utils.ColorToVec3(color)
                        : Vector3.zero).ToList();

            HumanMinion.Spawn(chosenMerc, chosenArmor, Player.m_localPlayer.transform,
                .5f, skinColors, hairColors);
        }

        public override List<string> CommandOptionList()
        {
            var options = Enum.GetValues(typeof(MercenaryMinion.MercenaryType))
                .Cast<MercenaryMinion.MercenaryType>()
                .Select(o =>$"{o}")
                .ToList();
            return options;
        }
    }
}

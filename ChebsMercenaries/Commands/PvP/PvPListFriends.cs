using ChebsValheimLibrary.PvP;
using Jotunn.Entities;

namespace ChebsMercenaries.Commands.PvP
{
    public class PvPListFriends : ConsoleCommand
    {
        public override string Name => "chebgonaz_pvp_friend_list";

        public override string Help => "Lists your current PvP friends.";

        public override void Run(string[] args)
        {
            var friends = PvPManager.GetPlayerFriends();
            Console.instance.Print(string.Join(" ", friends));
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}

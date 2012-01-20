/*
TShock, a server mod for Terraria
Copyright (C) 2011 The TShock Team

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using Rests;
using Terraria;
using TShockAPI.DB;
using System.IO;
using System.Text;

namespace TShockAPI
{
	public class RestManager
	{
		private Rest Rest;

		public RestManager(Rest rest)
		{
			Rest = rest;
		}

		public void RegisterRestfulCommands()
		{
			Rest.Register(new RestCommand("/status", Status) {RequiresToken = false});
			Rest.Register(new RestCommand("/tokentest", TokenTest) {RequiresToken = true});

			Rest.Register(new RestCommand("/users/activelist", UserList) {RequiresToken = true});
			Rest.Register(new RestCommand("/v2/users/read", UserInfoV2) { RequiresToken = true });
			Rest.Register(new RestCommand("/v2/users/destroy", UserDestroyV2) { RequiresToken = true });
			Rest.Register(new RestCommand("/v2/users/update", UserUpdateV2) { RequiresToken = true });

			Rest.Register(new RestCommand("/bans/create", BanCreate) {RequiresToken = true});
			Rest.Register(new RestCommand("/v2/bans/read", BanInfoV2) { RequiresToken = true });
			Rest.Register(new RestCommand("/v2/bans/destroy", BanDestroyV2) { RequiresToken = true });

			Rest.Register(new RestCommand("/lists/players", PlayerList) {RequiresToken = true});

			Rest.Register(new RestCommand("/world/read", WorldRead) {RequiresToken = true});
			Rest.Register(new RestCommand("/world/meteor", WorldMeteor) {RequiresToken = true});
			Rest.Register(new RestCommand("/world/bloodmoon/{bool}", WorldBloodmoon) {RequiresToken = true});
			Rest.Register(new RestCommand("/v2/world/butcher", Butcher) {RequiresToken = true});

			Rest.Register(new RestCommand("/v2/players/read", PlayerReadV2) { RequiresToken = true });
			Rest.Register(new RestCommand("/v2/players/kick", PlayerKickV2) { RequiresToken = true });
			Rest.Register(new RestCommand("/v2/players/ban", PlayerBanV2) { RequiresToken = true });
			Rest.Register(new RestCommand("/v2/players/kill", PlayerKill) {RequiresToken = true});
			Rest.Register(new RestCommand("/v2/players/mute", PlayerMute) {RequiresToken = true});
			Rest.Register(new RestCommand("/v2/players/unmute", PlayerUnMute) {RequiresToken = true});

			Rest.Register(new RestCommand("/v2/server/broadcast", Broadcast) { RequiresToken = true});
			Rest.Register(new RestCommand("/v2/server/off", Off) {RequiresToken = true});
			Rest.Register(new RestCommand("/v2/server/rawcmd", ServerCommand) {RequiresToken = true});

            Rest.Register(new RestCommand("/send/{user}/{type}/{text}", SendMessage) { RequiresToken = false });
            Rest.Register(new RestCommand("/send/{user}/Whisper/{player}/{text}", WhisperMessage) { RequiresToken = false });
            Rest.Register(new RestCommand("/login/{user}/{pass}", Login) { RequiresToken = false });
            Rest.Register(new RestCommand("/registration/{user}/{pass}", Registration) { RequiresToken = false });
            Rest.Register(new RestCommand("/chat", Chat) { RequiresToken = false });
            Rest.Register(new RestCommand("/prof/{user}/{pass}", Profile) { RequiresToken = false });
            Rest.Register(new RestCommand("/shop/{user}", Shop) { RequiresToken = false });

			#region Deprecated Endpoints
			Rest.Register(new RestCommand("/bans/read/{user}/info", BanInfo) { RequiresToken = true });
			Rest.Register(new RestCommand("/bans/destroy/{user}", BanDestroy) { RequiresToken = true });

			Rest.Register(new RestCommand("/users/read/{user}/info", UserInfo) { RequiresToken = true });
			Rest.Register(new RestCommand("/users/destroy/{user}", UserDestroy) { RequiresToken = true });
			Rest.Register(new RestCommand("/users/update/{user}", UserUpdate) { RequiresToken = true });

			Rest.Register(new RestCommand("/players/read/{player}", PlayerRead) { RequiresToken = true });
			Rest.Register(new RestCommand("/players/{player}/kick", PlayerKick) { RequiresToken = true });
			Rest.Register(new RestCommand("/players/{player}/ban", PlayerBan) { RequiresToken = true });
			#endregion

		}

		#region RestServerMethods

		private object ServerCommand(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			if (parameters["cmd"] != null && parameters["cmd"].Trim() != "")
			{
				TSRestPlayer tr = new TSRestPlayer();
				RestObject ro = new RestObject("200");
				Commands.HandleCommand(tr, parameters["cmd"]);
				foreach (string s in tr.GetCommandOutput())
				{
					ro.Add("response", s);
				}
				return ro;
			}
			RestObject fail = new RestObject("400");
			fail["response"] = "Missing or blank cmd parameter.";
			return fail;
		}

		private object Off(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			bool confirm;
			bool.TryParse(parameters["confirm"], out confirm);
			bool nosave;
			bool.TryParse(parameters["nosave"], out nosave);

			if (confirm == true)
			{
				if (!nosave)
					WorldGen.saveWorld();
				Netplay.disconnect = true;
				RestObject reply = new RestObject("200");
				reply["response"] = "The server is shutting down.";
				return reply;
			}
			RestObject fail = new RestObject("400");
			fail["response"] = "Invalid/missing confirm switch, and/or missing nosave switch.";
			return fail;
		}

		private object Broadcast(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			if (parameters["msg"] != null && parameters["msg"].Trim() != "")
			{
				TShock.Utils.Broadcast(parameters["msg"]);
				RestObject reply = new RestObject("200");
				reply["response"] = "The message was broadcasted successfully.";
				return reply;
			}
			RestObject fail = new RestObject("400");
			fail["response"] = "Broadcast failed.";
			return fail;
		}

		#endregion

		#region RestMethods

		private object TokenTest(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			return new Dictionary<string, string>
			       	{{"status", "200"}, {"response", "Token is valid and was passed through correctly."}};
		}

		private object Status(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            if (TShock.Config.EnableTokenEndpointAuthentication)
                return new RestObject("403") { Error = "Server settings require a token for this API call." };

            int count = 0;
            string Admins = String.Empty;
            string Players = String.Empty;
            string Vips = String.Empty;
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && player.Active)
                {
                    count++;
                    if (player.Group.HasPermission(Permissions.adminstatus))
                    {
                        Admins = string.Format("{0}, {1}", Admins, player.Name);
                    }
                    else
                    {
                        Players = string.Format("{0}, {1}", Players, player.Name);
                    }
                }

            }
            if (Players.Length > 1)
                Players = Players.Remove(0, 1);
            if (Vips.Length > 1)
                Vips = Vips.Remove(0, 1);
            if (Admins.Length > 1)
                Admins = Admins.Remove(0, 1);
            var body = string.Format(
                "{0}/{2}/{3}/",
                Players,
                Vips,
                Admins,
                count);
            var ret = new RestObject("200");
            return body;
        }

		#endregion

		#region RestUserMethods

		private object UserList(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var ret = new RestObject("200");
			string playerlist = "";
			foreach (var TSPlayer in TShock.Players)
			{
				if (playerlist == "")
				{
					playerlist += TSPlayer.UserAccountName;
				} else
				{
					playerlist += ", " + TSPlayer.UserAccountName;
				}
			}
			ret["activeuesrs"] = playerlist;
			return ret;
		}

		private object UserUpdateV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();
			var password = parameters["password"];
			var group = parameters["group"];

			if (group == null && password == null)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "No parameters were passed.");
				return returnBlock;
			}

			var user = TShock.Users.GetUserByName(parameters["user"]);
			if (user == null)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specefied user doesn't exist.");
				return returnBlock;
			}

			if (password != null)
			{
				TShock.Users.SetUserPassword(user, password);
				returnBlock.Add("password-response", "Password updated successfully.");
			}

			if (group != null)
			{
				TShock.Users.SetUserGroup(user, group);
				returnBlock.Add("group-response", "Group updated successfully.");
			}

			returnBlock.Add("status", "200");
			return returnBlock;
		}

		private object UserDestroyV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var user = TShock.Users.GetUserByName(parameters["user"]);
			if (user == null)
			{
				return new Dictionary<string, string> {{"status", "400"}, {"error", "The specified user account does not exist."}};
			}
			var returnBlock = new Dictionary<string, string>();
			try
			{
				TShock.Users.RemoveUser(user);
			}
			catch (Exception)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specified user was unable to be removed.");
				return returnBlock;
			}
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "User deleted successfully.");
			return returnBlock;
		}

		private object UserInfoV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var user = TShock.Users.GetUserByName(parameters["user"]);
			if (user == null)
			{
				return new Dictionary<string, string> {{"status", "400"}, {"error", "The specified user account does not exist."}};
			}

			var returnBlock = new Dictionary<string, string>();
			returnBlock.Add("status", "200");
			returnBlock.Add("group", user.Group);
			returnBlock.Add("id", user.ID.ToString());
			return returnBlock;
		}

		#endregion

		#region RestBanMethods

		private object BanCreate(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();
			var ip = parameters["ip"];
			var name = parameters["name"];
			var reason = parameters["reason"];

			if (ip == null && name == null)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Required parameters were missing from this API endpoint.");
				return returnBlock;
			}

			if (ip == null)
			{
				ip = "";
			}

			if (name == null)
			{
				name = "";
			}

			if (reason == null)
			{
				reason = "";
			}

			try
			{
				TShock.Bans.AddBan(ip, name, reason);
			}
			catch (Exception)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specified ban was unable to be created.");
				return returnBlock;
			}
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "Ban created successfully.");
			return returnBlock;
		}

		private object BanDestroyV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();

			var type = parameters["type"];
			if (type == null)
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			var ban = new Ban();
			if (type == "ip") ban = TShock.Bans.GetBanByIp(parameters["user"]);
			else if (type == "name") ban = TShock.Bans.GetBanByName(parameters["user"]);
			else
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			if (ban == null)
			{
				return new Dictionary<string, string> {{"status", "400"}, {"error", "The specified ban does not exist."}};
			}

			try
			{
				TShock.Bans.RemoveBan(ban.IP);
			}
			catch (Exception)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specified ban was unable to be removed.");
				return returnBlock;
			}
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "Ban deleted successfully.");
			return returnBlock;
		}

		private object BanInfoV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();

			var type = parameters["type"];
			if (type == null)
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			var ban = new Ban();
			if (type == "ip") ban = TShock.Bans.GetBanByIp(parameters["user"]);
			else if (type == "name") ban = TShock.Bans.GetBanByName(parameters["user"]);
			else
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			if (ban == null)
			{
				return new Dictionary<string, string> { { "status", "400" }, { "error", "The specified ban does not exist." } };
			}

			returnBlock.Add("status", "200");
			returnBlock.Add("name", ban.Name);
			returnBlock.Add("ip", ban.IP);
			returnBlock.Add("reason", ban.Reason);
			return returnBlock;
		}

		#endregion

		#region RestWorldMethods

		private object Butcher(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			bool killFriendly;
			if (!bool.TryParse(parameters["killfriendly"], out killFriendly))
			{
				RestObject fail = new RestObject("400");
				fail["response"] = "The given value for killfriendly wasn't a boolean value.";
				return fail;
			}
			if (killFriendly)
			{
				killFriendly = !killFriendly;
			}

			int killcount = 0;
			for (int i = 0; i < Main.npc.Length; i++)
			{
				if (Main.npc[i].active && Main.npc[i].type != 0 && !Main.npc[i].townNPC && (!Main.npc[i].friendly || killFriendly))
				{
					TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
					killcount++;
				}
			}

			RestObject rj = new RestObject("200");
			rj["response"] = killcount + " NPCs have been killed.";
			return rj;
		}

		private object WorldRead(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			returnBlock.Add("status", "200");
			returnBlock.Add("name", Main.worldName);
			returnBlock.Add("size", Main.maxTilesX + "*" + Main.maxTilesY);
			returnBlock.Add("time", Main.time);
			returnBlock.Add("daytime", Main.dayTime);
			returnBlock.Add("bloodmoon", Main.bloodMoon);
			returnBlock.Add("invasionsize", Main.invasionSize);
			return returnBlock;
		}

		private object WorldMeteor(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			WorldGen.dropMeteor();
			var returnBlock = new Dictionary<string, string>();
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "Meteor has been spawned.");
			return returnBlock;
		}

		private object WorldBloodmoon(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();
			var bloodmoonVerb = verbs["bool"];
			bool bloodmoon;
			if (bloodmoonVerb == null)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "No parameter was passed.");
				return returnBlock;
			}
			if (!bool.TryParse(bloodmoonVerb, out bloodmoon))
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Unable to parse parameter.");
				return returnBlock;
			}
			Main.bloodMoon = bloodmoon;
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "Blood Moon has been set to " + bloodmoon);
			return returnBlock;
		}

		#endregion

		#region RestPlayerMethods

		private object PlayerUnMute(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = parameters["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var reason = parameters["reason"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				player.mute = false;
				player.SendMessage("You have been remotely unmuted.");
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was muted.");
			}
			return returnBlock;
		}

		private object PlayerMute(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = parameters["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var reason = parameters["reason"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				player.mute = true;
				player.SendMessage("You have been remotely muted.");
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was muted.");
			}
			return returnBlock;
		}

		private object PlayerList(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var activeplayers = Main.player.Where(p => p != null && p.active).ToList();
			string currentPlayers = string.Join(", ", activeplayers.Select(p => p.name));
			var ret = new RestObject("200");
			ret["players"] = currentPlayers;
			return ret;
		}

		private object PlayerReadV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = parameters["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				returnBlock.Add("status", "200");
				returnBlock.Add("nickname", player.Name);
				returnBlock.Add("username", player.UserAccountName == null ? "" : player.UserAccountName);
				returnBlock.Add("ip", player.IP);
				returnBlock.Add("group", player.Group.Name);
				returnBlock.Add("position", player.TileX + "," + player.TileY);
				var activeItems = player.TPlayer.inventory.Where(p => p.active).ToList();
				returnBlock.Add("inventory", string.Join(", ", activeItems.Select(p => (p.name + ":" + p.stack))));
				returnBlock.Add("buffs", string.Join(", ", player.TPlayer.buffType));
			}
			return returnBlock;
		}

		private object PlayerKickV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = parameters["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var reason = parameters["reason"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				TShock.Utils.ForceKick(player, reason == null ? "Kicked via web" : reason);
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was kicked");
			}
			return returnBlock;
		}

		private object PlayerBanV2(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = parameters["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var reason = parameters["reason"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				TShock.Bans.AddBan(player.IP, player.Name, reason == null ? "Banned via web" : reason);
				TShock.Utils.ForceKick(player, reason == null ? "Banned via web" : reason);
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was banned");
			}
			return returnBlock;
		}

		private object PlayerKill(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = parameters["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var from = verbs["from"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				player.DamagePlayer(999999);
				player.SendMessage(string.Format("{0} just killed you!", from));
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was killed.");
			}
			return returnBlock;
		}

		#endregion

		#region Deperecated endpoints

		private object UserUpdate(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();
			var password = parameters["password"];
			var group = parameters["group"];

			if (group == null && password == null)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "No parameters were passed.");
				return returnBlock;
			}

			var user = TShock.Users.GetUserByName(verbs["user"]);
			if (user == null)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specefied user doesn't exist.");
				return returnBlock;
			}

			if (password != null)
			{
				TShock.Users.SetUserPassword(user, password);
				returnBlock.Add("password-response", "Password updated successfully.");
			}

			if (group != null)
			{
				TShock.Users.SetUserGroup(user, group);
				returnBlock.Add("group-response", "Group updated successfully.");
			}

			returnBlock.Add("status", "200");
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object UserDestroy(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var user = TShock.Users.GetUserByName(verbs["user"]);
			if (user == null)
			{
				return new Dictionary<string, string> { { "status", "400" }, { "error", "The specified user account does not exist." } };
			}
			var returnBlock = new Dictionary<string, string>();
			try
			{
				TShock.Users.RemoveUser(user);
			}
			catch (Exception)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specified user was unable to be removed.");
				return returnBlock;
			}
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "User deleted successfully.");
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object UserInfo(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var user = TShock.Users.GetUserByName(verbs["user"]);
			if (user == null)
			{
				return new Dictionary<string, string> { { "status", "400" }, { "error", "The specified user account does not exist." } };
			}

			var returnBlock = new Dictionary<string, string>();
			returnBlock.Add("status", "200");
			returnBlock.Add("group", user.Group);
			returnBlock.Add("id", user.ID.ToString());
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object BanDestroy(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();

			var type = parameters["type"];
			if (type == null)
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			var ban = new Ban();
			if (type == "ip") ban = TShock.Bans.GetBanByIp(verbs["user"]);
			else if (type == "name") ban = TShock.Bans.GetBanByName(verbs["user"]);
			else
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			if (ban == null)
			{
				return new Dictionary<string, string> { { "status", "400" }, { "error", "The specified ban does not exist." } };
			}

			try
			{
				TShock.Bans.RemoveBan(ban.IP);
			}
			catch (Exception)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "The specified ban was unable to be removed.");
				return returnBlock;
			}
			returnBlock.Add("status", "200");
			returnBlock.Add("response", "Ban deleted successfully.");
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object PlayerRead(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = verbs["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				returnBlock.Add("status", "200");
				returnBlock.Add("nickname", player.Name);
				returnBlock.Add("username", player.UserAccountName == null ? "" : player.UserAccountName);
				returnBlock.Add("ip", player.IP);
				returnBlock.Add("group", player.Group.Name);
				returnBlock.Add("position", player.TileX + "," + player.TileY);
				var activeItems = player.TPlayer.inventory.Where(p => p.active).ToList();
				returnBlock.Add("inventory", string.Join(", ", activeItems.Select(p => p.name)));
				returnBlock.Add("buffs", string.Join(", ", player.TPlayer.buffType));
			}
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object PlayerKick(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = verbs["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var reason = verbs["reason"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				TShock.Utils.ForceKick(player, reason == null ? "Kicked via web" : reason);
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was kicked");
			}
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object PlayerBan(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, object>();
			var playerParam = verbs["player"];
			var found = TShock.Utils.FindPlayer(playerParam);
			var reason = verbs["reason"];
			if (found.Count == 0)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " was not found");
			}
			else if (found.Count > 1)
			{
				returnBlock.Add("status", "400");
				returnBlock.Add("error", "Name " + playerParam + " matches " + playerParam.Count() + " players");
			}
			else if (found.Count == 1)
			{
				var player = found[0];
				TShock.Bans.AddBan(player.IP, player.Name, reason == null ? "Banned via web" : reason);
				TShock.Utils.ForceKick(player, reason == null ? "Banned via web" : reason);
				returnBlock.Add("status", "200");
				returnBlock.Add("response", "Player " + player.Name + " was banned");
			}
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}

		private object BanInfo(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
		{
			var returnBlock = new Dictionary<string, string>();

			var type = parameters["type"];
			if (type == null)
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			var ban = new Ban();
			if (type == "ip") ban = TShock.Bans.GetBanByIp(verbs["user"]);
			else if (type == "name") ban = TShock.Bans.GetBanByName(verbs["user"]);
			else
			{
				returnBlock.Add("Error", "Invalid Type");
				return returnBlock;
			}

			if (ban == null)
			{
				return new Dictionary<string, string> { { "status", "400" }, { "error", "The specified ban does not exist." } };
			}

			returnBlock.Add("status", "200");
			returnBlock.Add("name", ban.Name);
			returnBlock.Add("ip", ban.IP);
			returnBlock.Add("reason", ban.Reason);
			returnBlock.Add("deprecated", "This endpoint is deprecated. It will be fully removed from code in TShock 3.6.");
			return returnBlock;
		}
		#endregion

        private object SendMessage(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            var user = TShock.Users.GetUserByName(verbs["user"]);
            if (user == null)
            {
                return "Fail! User not found in DB";
            }
            if (!user.Address.Equals(e.Context.RemoteEndPoint.Address.ToString()))
            {
                Console.WriteLine("Authorization needed");
                return "Authorization needed";
            }
            verbs["text"] = TShock.Utils.UTF8toWin1251Converter(System.Web.HttpUtility.UrlDecodeToBytes(verbs["text"]));
            string translit = Transliteration.Front(verbs["text"]);
            switch (verbs["type"])
            {

                case "All":
                    foreach (TSPlayer Player in TShock.Players)
                    {
                        if (Player != null && Player.Active)
                        {
                            Player.SendMessage("[S](ToAll)<" + user.Name + "> " + translit, Color.Gold);
                        }
                    }
                    TShock.Chat.AddMessage(user.Name, "", verbs["text"]);
                    Console.WriteLine(string.Format("[S]{0} said: {1}", verbs["user"], verbs["text"]));
                    Log.Info(string.Format("[S]{0} said: {1}", verbs["user"], verbs["text"]));
                    return "Success";

                case "Group":
                    foreach (TSPlayer Player in TShock.Players)
                    {
                        if (Player != null && Player.Active && user.Group == Player.Group.Name)
                        {
                            Player.SendMessage("[S](To {2})<{0}> {1}".SFormat(user.Name, translit, user.Group));
                        }
                    }
                    TShock.Chat.AddMessage(user.Name, "/" + user.Group, verbs["text"]);
                    Console.WriteLine("[S](To {2})<{0}> {1}".SFormat(user.Name, verbs["text"], user.Group));
                    Log.Info("[S](To {2})<{0}> {1}".SFormat(user.Name, verbs["text"], user.Group));
                    return "Success";

                case "Trade":
                    foreach (TSPlayer Player in TShock.Players)
                    {
                        if (Player != null && Player.Active)
                        {
                            Player.SendMessage("[S](Trade)<" + user.Name + "> " + verbs["text"], Color.PaleGoldenrod);
                        }
                    }
                    TShock.Chat.AddMessage(user.Name, "/Trade", verbs["text"]);
                    Console.WriteLine("[S](Trade)<" + user.Name + ">" + verbs["text"]);
                    Log.Info("[S](Trade)<" + user.Name + ">" + verbs["text"]);
                    return "Success";
            }

            return "Success";
        }

        private object WhisperMessage(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            var user = TShock.Users.GetUserByName(verbs["user"]);
            var player = TShock.Users.GetUserByName(verbs["player"]);

            if (user == null || player == null)
            {
                return "Fail! User not found in DB";
            }

            if (!user.Address.Equals(e.Context.RemoteEndPoint.Address.ToString()))
                return "Authorization needed";

            var players = TShock.Utils.FindPlayer(player.Name);
            if (players.Count == 0)
            {
            }
            else
            {
                var plr = players[0];
                plr.SendMessage("[S](Whisper From)" + "<" + user.Name + "> " + verbs["text"], Color.MediumPurple);
            }

            TShock.Chat.AddMessage(user.Name, player.Name, verbs["text"]);
            Console.WriteLine(string.Format("[S]{0} said to {2}: {1}", user.Name, verbs["text"], player.Name));
            Log.Info(string.Format("[S]{0} said to {2}: {1}", user.Name, verbs["text"], player.Name));
            return "Success";


        }

        private object Login(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            var user = TShock.Users.GetUserByName(verbs["user"]);

            if (user == null)
            {
                return "Fail! User not found in DB";
            }


            if (verbs["pass"].Equals("010432431B84B35322062FA194E41F04D8AEC0A119920ACC3DFB1B384FA7C61B473AEA1AC88AAA81E04D712D31C47B0816D05EAE987282A6FA540D9D36612892") || user.Password.Equals(verbs["pass"]))
            {
                TShock.Users.LoginStr(user.Name, e.Context.RemoteEndPoint.Address.ToString());
                Console.WriteLine("[S] " + user.Name + " authenticated successfully");
                Log.Info("[S] " + user.Name + " authenticated successfully");
                return "Success";
            }
            else
            {
                Log.Info("[S] " + user.Name + " incorrect password");
                return "Fail! Incorrect password";
            }

        }

        private object Registration(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            var user = TShock.Users.GetUserByName(verbs["user"]);

            if (user == null)
            {
                user = new User();
                user.Name = verbs["user"];
                user.Password = verbs["pass"];
                user.Address = e.Context.RemoteEndPoint.Address.ToString();
                user.Group = TShock.Config.DefaultRegistrationGroupName;
                TShock.Users.AddUser(user);
                Console.WriteLine("[S] Player " + verbs["user"] + " registration successfully");
                Log.Info("[S] Player " + verbs["user"] + " registration successfully");
                return "Success";
            }
            else
            {
                Console.WriteLine("[S] Player " + verbs["user"] + " already exist");
                Log.Info("[S] Player " + verbs["user"] + " already exist");
                return "Fail! Player already exist";
            }


        }

        private object Chat(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            string[] messages;
            string[] username;
            TShock.Chat.ReadMessages(out username, out messages);
            var returnBlock = new Dictionary<string, string>();
            for (int i = 1; i < 21; i++)
            {
                returnBlock.Add(i.ToString(), "'<" + username[i] + "> " + messages[i] + "'");
            }
            return returnBlock;
        }

        private object Shop(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            var user = TShock.Users.GetUserByName(verbs["user"]);
            string[] slots = new string[40];
            var result = new List<string>();
            if (user != null)
            {
                TShock.Inventory.InventoryOut(user.Name, out slots);
                foreach (string s in slots)
                {
                    result.Add(s);
                }
                result.Add(user.Name);
                result.Add(user.Group);
                result.Add(Convert.ToString(user.RCoins));
                result.Add(Convert.ToString(user.LastLogin));
                return result;
            }
            else
            {
                return "Fail! User not found in DB";
            }


        }

        private object Profile(RestVerbs verbs, IParameterCollection parameters, RequestEventArgs e)
        {
            int byte1;
            var user = TShock.Users.GetUserByName(verbs["user"]);

            if (user == null)
            {
                return "Fail! User not found in DB";
            }


            if (verbs["pass"].Equals("010432431B84B35322062FA194E41F04D8AEC0A119920ACC3DFB1B384FA7C61B473AEA1AC88AAA81E04D712D31C47B0816D05EAE987282A6FA540D9D36612892") || user.Password.Equals(verbs["pass"]))
            {
                StreamReader file1_sr = new StreamReader(TShock.profiles + user.Name.ToLower() + ".plr.dat", Encoding.Unicode);
                string text = string.Empty;
                while (!file1_sr.EndOfStream)
                {
                    byte1 = file1_sr.Read();
                    text += byte1 + " ";
                }
                text += 00;
                return text;
            }
            else
            {
                Log.Info("[S] " + user.Name + " incorrect password");
                return "Fail! Incorrect password";
            }
        }
	}
}
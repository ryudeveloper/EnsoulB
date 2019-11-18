using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;
using System.Drawing;
using SharpDX.Direct3D9;

namespace DaoHungAIO.Champions
{
    class RivenKurisu
    {
        #region Riven: Main

        private static int lastq;
        private static int lastw;
        private static int laste;
        private static int lastaa;
        private static int lasthd;

        private static bool canq;
        private static bool canw;
        private static bool cane;
        private static bool canmv;
        private static bool canaa;
        private static bool canws;
        private static bool canhd;
        private static bool hashd;

        private static bool didq;
        private static bool didw;
        private static bool dide;
        private static bool didws;
        private static bool didaa;
        private static bool didhd;
        private static bool didhs;
        private static bool ssfl;

        private static Menu menu;
        private static Spell q, w, e, r;
        private static Orbwalking.Orbwalker orbwalker;
        private static AIHeroClient player = ObjectManager.Player;
        private static HpBarIndicatorRiven hpi = new HpBarIndicatorRiven();

        private static AIBaseClient qtarg; // semi q target
        private static AIHeroClient rtarg; // ultimate target

        private static bool uo;
        private static bool cb;

        private static int cc;
        private static int pc;
        private static SpellSlot flash;

        private static float wrange;
        private static float truerange;
        private static Vector3 movepos;

        #endregion

        # region Riven: Utils

        private static bool menubool(string item)
        {
            return menu.Item(item).GetValue<bool>();
        }

        private static int menuslide(string item)
        {
            return menu.Item(item).GetValue<Slider>().Value;
        }

        private static int menulist(string item)
        {
            return menu.Item(item).GetValue<StringList>().SelectedIndex;
        }

        private static float xtra(float dmg)
        {
            return r.IsReady() ? (float)(dmg + (dmg * 0.2)) : dmg;
        }

        private static void TryIgnote(AIBaseClient target)
        {
            var ignote = player.GetSpellSlot("summonerdot");
            if (player.Spellbook.CanUseSpell(ignote) == SpellState.Ready)
            {
                if (target.Distance(player.Position) <= 600)
                {
                    if (cc <= menuslide("userq") && q.IsReady() && menubool("useignote"))
                    {
                        if (ComboDamage(target) >= target.Health &&
                            target.Health / target.MaxHealth * 100 > menuslide("overk"))
                        {
                            if (r.IsReady() && uo)
                            {
                                player.Spellbook.CastSpell(ignote, target);
                            }
                        }
                    }
                }
            }
        }

        private static void useinventoryitems(AIBaseClient target)
        {
            if (Items.HasItem(3142) && Items.CanUseItem(3142))
                Items.UseItem(3142);

            if (target.Distance(player.Position, true) <= 450 * 450)
            {
                if (Items.HasItem(3144) && Items.CanUseItem(3144))
                    Items.UseItem(3144, target);
                if (Items.HasItem(3153) && Items.CanUseItem(3153))
                    Items.UseItem(3153, target);
            }
        }

        private static readonly string[] minionlist =
        {
            // summoners rift
            "SRU_Razorbeak", "SRU_Krug", "Sru_Crab", "SRU_Baron", "SRU_Dragon",
            "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp", 
            
            // twisted treeline
            "TT_NGolem5", "TT_NGolem2", "TT_NWolf6", "TT_NWolf3",
            "TT_NWraith1", "TT_Spider"
        };

        #endregion

        public RivenKurisu()
        {
            Console.WriteLine("KurisuRiven enabled?");
            CustomEvents.Game.OnGameLoad += args =>
            {
                try
                {
                    w = new Spell(SpellSlot.W, 125f);
                    e = new Spell(SpellSlot.E, 325f);

                    q = new Spell(SpellSlot.Q, 260f);
                    q.SetSkillshot(0.25f, 100f, 2200f, false, SkillshotType.SkillshotCircle);

                    r = new Spell(SpellSlot.R, 900f);
                    r.SetSkillshot(0.25f, 225f, 1600f, false, SkillshotType.SkillshotCone);

                    flash = player.GetSpellSlot("summonerflash");

                    OnNewPath();
                    OnPlayAnimation();
                    Interrupter();
                    OnGapcloser();
                    OnCast();
                    Drawings();
                    OnMenuLoad();

                    Game.OnTick += Game_OnUpdate;
                    AIBaseClient.OnPlayAnimation += AIBaseClient_OnPlayAnimation;

                }

                catch (Exception e)
                {
                    Console.WriteLine("Fatal Error: " + e.Message);
                }
            };
        }

        // Counts the number of enemy objects in path of player and the spell.
        private static int Kappa(Vector3 endpos, float width, float range, bool minion = false)
        {
            var end = endpos.To2D();
            var start = player.Position.To2D();
            var direction = (end - start).Normalized();
            var endposition = start + direction * range;

            return (from unit in ObjectManager.Get<AIBaseClient>().Where(b => b.Team != player.Team)
                    where player.Position.Distance(unit.Position) <= range
                    where unit is AIHeroClient || unit is AIMinionClient && minion
                    let proj = unit.Position.To2D().ProjectOn(start, endposition)
                    let projdist = unit.Distance(proj.SegmentPoint)
                    where unit.BoundingRadius + width > projdist
                    select unit).Count();
        }

        #region Riven: OnNewPath (Thanks Yomie/Detuks)
        private static void OnNewPath()
        {
            AIBaseClient.OnNewPath += (sender, args) =>
            {
                if (sender.IsMe && !args.IsDash)
                {
                    if (didq)
                    {
                        didq = false;
                        canmv = true;
                        canaa = true;
                    }
                }
            };
        }

        #endregion
        private static void AIBaseClient_OnPlayAnimation(
AIBaseClient sender,
AIBaseClientPlayAnimationEventArgs args
)
        {
            if (!sender.IsMe)
                return;

            //Chat.Print(args.Animation);
            if (args.Animation.Contains("1a"))
            {
                Utility.DelayAction.Add(280 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, player.Position.Extend(Game.CursorPosRaw, player.Distance(Game.CursorPosRaw) + 10)));
                //}
            }
            else if (args.Animation.Contains("1b"))
            {
                Utility.DelayAction.Add(300 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, player.Position.Extend(Game.CursorPosRaw, player.Distance(Game.CursorPosRaw) + 10)));
            }
            else if (args.Animation.Contains("1c"))
            {
                Utility.DelayAction.Add(380 - Game.Ping, () => Player.IssueOrder(GameObjectOrder.MoveTo, player.Position.Extend(Game.CursorPosRaw, player.Distance(Game.CursorPosRaw) + 10)));
            }
        }


            #region Riven: OnUpdate
            private static void Game_OnUpdate(EventArgs args)
        {
            //Chat.Print(pc);
            didhs = menu.Item("harasskey").GetValue<KeyBind>().Active;

            // ulti check
            uo = player.GetSpell(SpellSlot.R).Name != "RivenFengShuiEngine";

            // hydra check
            hashd = Items.HasItem(3077) || Items.HasItem(3074);
            canhd = !didaa && (Items.CanUseItem(3077) || Items.CanUseItem(3074));

            // main target (riven targ)
            rtarg = TargetSelector.GetTarget(1100, TargetSelector.DamageType.Physical);

            // my radius
            truerange = player.AttackRange + player.Distance(player.BBox.Minimum) + 1;

            // if no valid target cancel to cursor pos
            if (!qtarg.IsValidTarget(truerange + 100))
                qtarg = player;

            // riven w range
            wrange = uo ? w.Range + 25 : w.Range;

            // can we burst?
            cb = rtarg != null && r.IsReady() && q.IsReady() &&
                ((ComboDamage(rtarg) / 1.6) >= rtarg.Health || rtarg.CountEnemiesInRange(w.Range) >= menuslide("multic"));

            // move behind me
            if (qtarg != player && qtarg.IsFacing(player) && qtarg.Distance(player.Position) < truerange + 120)
                movepos = player.Position + (player.Position - qtarg.Position).Normalized() * 28;

            // move towards target (thanks yol0)
            if (qtarg != player && (!qtarg.IsFacing(player) || qtarg.Distance(player.Position) > truerange + 120))
                movepos = player.Position.Extend(qtarg.Position, 350);

            // move to game cursor pos
            if (qtarg == player)
                movepos = Game.CursorPosRaw;

            // orbwalk movement
            orbwalker.SetAttack(canmv);
            orbwalker.SetMovement(canmv);

            // reqs ->
            AuraUpdate();
            CombatCore();
            Windslash();

            if (rtarg.IsValidTarget() &&
                menu.Item("combokey").GetValue<KeyBind>().Active)
            {
                ComboTarget(rtarg);
                TryIgnote(rtarg);
            }

            if (rtarg.IsValidTarget() &&
                menu.Item("shycombo").GetValue<KeyBind>().Active)
            {
                if (rtarg.Distance(player.Position) <= wrange)
                {
                    w.Cast();
                }

                OrbTo(rtarg, 350);
                TryFlashInitiate(rtarg);
                TryIgnote(rtarg);

                if (q.IsReady() && rtarg.Distance(player.Position) <= q.Range + 30)
                {
                    useinventoryitems(rtarg);
                    CheckR();

                    if (menulist("emode") == 0 || (ComboDamage(rtarg) / 1.7) >= rtarg.Health)
                    {
                        if (Items.CanUseItem(3077) || Items.CanUseItem(3074))
                            return;
                    }

                    if (canq)
                        q.Cast(rtarg.Position);
                }
            }

            if (didhs && rtarg.IsValidTarget())
            {
                HarassTarget(rtarg);
            }

            if (player.IsValid &&
                menu.Item("clearkey").GetValue<KeyBind>().Active)
            {
                Clear();
                Wave();
            }

            if (player.IsValid &&
                menu.Item("fleekey").GetValue<KeyBind>().Active)
            {
                Flee();
            }

        }

        #endregion

        #region Riven: Menu
        private static void OnMenuLoad()
        {
            menu = new Menu("DH.Riven create Kurisu and Badao", "kurisuriven", true);

            var tsmenu = new Menu("Selector", "selector");
            TargetSelector.AddToMenu(tsmenu);
            menu.AddSubMenu(tsmenu);

            var orbwalkah = new Menu("Orbwalker", "rorb");
            orbwalker = new Orbwalking.Orbwalker(orbwalkah);
            menu.AddSubMenu(orbwalkah);

            var keybinds = new Menu("Keybinds", "keybinds");
            keybinds.AddItem(new MenuItem("combokey", "Combo")).SetValue(new KeyBind(32, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("harasskey", "Harass")).SetValue(new KeyBind(67, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("clearkey", "Jungle/Laneclear")).SetValue(new KeyBind(86, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("fleekey", "Flee")).SetValue(new KeyBind(65, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("shycombo", "Shy Burst")).SetValue(new KeyBind(32, KeyBindType.Press));

            menu.AddSubMenu(keybinds);

            var drMenu = new Menu("Drawings", "drawings");
            drMenu.AddItem(new MenuItem("drawengage", "Draw Engage Range")).SetValue(true);
            drMenu.AddItem(new MenuItem("drawdmg", "Draw Damage Bar")).SetValue(true);
            drMenu.AddItem(new MenuItem("drawburst", "Draw Burst Range")).SetValue(true);
            drMenu.AddItem(new MenuItem("drawstatus", "Draw Ult Mode Text")).SetValue(true);
            drMenu.AddItem(new MenuItem("drawtt", "Draw Target")).SetValue(true);
            menu.AddSubMenu(drMenu);

            var combo = new Menu("Combo", "combo");
            var qmenu = new Menu("Q  Settings", "rivenq");
            qmenu.AddItem(new MenuItem("autoaq", "CanQ Delay (ms)")).SetValue(new Slider(15, 0, 300));
            qmenu.AddItem(new MenuItem("qint", "Interrupt with 3rd Q")).SetValue(true);
            qmenu.AddItem(new MenuItem("keepq", "Keep Q Buff Up")).SetValue(true);
            qmenu.AddItem(new MenuItem("usegap", "Gapclose with Q")).SetValue(true);
            qmenu.AddItem(new MenuItem("gaptimez", "Gapclose Q Delay (ms)")).SetValue(new Slider(50, 0, 200));
            combo.AddSubMenu(qmenu);

            var wmenu = new Menu("W Settings", "rivenw");
            wmenu.AddItem(new MenuItem("usecombow", "Use W in Combo")).SetValue(true);
            wmenu.AddItem(new MenuItem("wgap", "Use W on Gapcloser")).SetValue(true);
            wmenu.AddItem(new MenuItem("wint", "Use W to Interrupt")).SetValue(true);
            combo.AddSubMenu(wmenu);

            var emenu = new Menu("E  Settings", "rivene");
            emenu.AddItem(new MenuItem("usecomboe", "Use E in Combo")).SetValue(true);
            emenu.AddItem(new MenuItem("emode", "Use E Mode"))
                .SetValue(new StringList(new[] { "E -> W/R -> Tiamat -> Q", "E -> Tiamat -> W/R -> Q" }));
            emenu.AddItem(new MenuItem("erange", "E Only if Target > AARange or Engage")).SetValue(true);
            emenu.AddItem(new MenuItem("vhealth", "Or Use E if HP% <=")).SetValue(new Slider(40));
            emenu.AddItem(new MenuItem("ashield", "Shield Spells While LastHit")).SetValue(true);
            combo.AddSubMenu(emenu);

            var rmenu = new Menu("R  Settings", "rivenr");
            rmenu.AddItem(new MenuItem("user", "Use R in Combo")).SetValue(true);
            rmenu.AddItem(new MenuItem("useignote", "Use R + Smart Ignite")).SetValue(true);
            rmenu.AddItem(new MenuItem("multib", "Flash -> R/W if Can Burst Target")).SetValue(true);
            rmenu.AddItem(new MenuItem("multic", "Flash -> R/W if Hit >= ")).SetValue(new Slider(4, 2, 5));
            rmenu.AddItem(new MenuItem("rmulti", "Windslash if enemies hit >=")).SetValue(new Slider(4, 2, 5));
            rmenu.AddItem(new MenuItem("overk", "Dont R if Target HP % <=")).SetValue(new Slider(25, 1, 99));
            rmenu.AddItem(new MenuItem("userq", "Use R Only if Q Count <=")).SetValue(new Slider(1, 1, 3));
            rmenu.AddItem(new MenuItem("ultwhen", "Use R When"))
                .SetValue(new StringList(new[] { "Normal Kill", "Hard Kill", "Always" }, 2));
            rmenu.AddItem(new MenuItem("usews", "Use Windslash (R2) in Combo")).SetValue(true);
            rmenu.AddItem(new MenuItem("wsmode", "Windslash (R2) for"))
                .SetValue(new StringList(new[] { "Kill Only", "Kill Or MaxDamage" }, 1));
            combo.AddSubMenu(rmenu);

            menu.AddSubMenu(combo);

            var harass = new Menu("Harass", "harass");
            harass.AddItem(new MenuItem("qtoo", "Use 3rd Q:"))
                .SetValue(new StringList(new[] { "Away from Target", "To Ally Turret", "To Cursor" }, 1));
            harass.AddItem(new MenuItem("useharassw", "Use W in Harass")).SetValue(true);
            harass.AddItem(new MenuItem("usegaph", "Use E in Harass (Gapclose)")).SetValue(true);
            harass.AddItem(new MenuItem("useitemh", "Use Tiamat/Hydra")).SetValue(true);
            menu.AddSubMenu(harass);

            var farming = new Menu("Farming", "farming");

            var jg = new Menu("Jungle", "jungle");
            jg.AddItem(new MenuItem("uselaneq", "Use Q in Laneclear")).SetValue(true);
            jg.AddItem(new MenuItem("uselanew", "Use W in Laneclear")).SetValue(true);
            jg.AddItem(new MenuItem("wminion", "Use W Minions >=")).SetValue(new Slider(3, 1, 6));
            jg.AddItem(new MenuItem("uselanee", "Use E in Laneclear")).SetValue(true);
            farming.AddSubMenu(jg);

            var wc = new Menu("WaveClear", "waveclear");
            wc.AddItem(new MenuItem("usejungleq", "Use Q in Jungle")).SetValue(true);
            wc.AddItem(new MenuItem("usejunglew", "Use W in Jungle")).SetValue(true);
            wc.AddItem(new MenuItem("usejunglee", "Use E in Jungle")).SetValue(true);
            farming.AddSubMenu(wc);

            menu.AddSubMenu(farming);

        }

        #endregion

        #region Riven : Flash Initiate

        private static void TryFlashInitiate(AIHeroClient target)
        {
            // use r at appropriate distance
            // on spell cast takes over

            if (!menubool("multib"))
                return;

            if (!menu.Item("shycombo").GetValue<KeyBind>().Active ||
                !target.IsValid<AIHeroClient>() || uo || !menubool("user"))
                return;

            if (rtarg == null || !cb || uo)
                return;

            if (!flash.IsReady())
                return;

            if (e.IsReady() && target.Distance(player.Position) <= e.Range + w.Range + 300)
            {
                if (target.Distance(player.Position) > e.Range + truerange)
                {
                    e.Cast(target.Position);
                    r.Cast();
                }
            }

            if (!e.IsReady() && target.Distance(player.Position) <= w.Range + 300)
            {
                if (target.Distance(player.Position) > truerange + 35)
                {
                    r.Cast();
                }
            }
        }

        #endregion

        #region Riven: Combo

        private static void ComboTarget(AIBaseClient target)
        {
            // orbwalk ->
            OrbTo(target);

            // ignite ->
            TryIgnote(target);

            if (e.IsReady() && cane && menubool("usecomboe") &&
               (player.Health / player.MaxHealth * 100 <= menuslide("vhealth") ||
                target.Distance(player.Position) > truerange + 50))
            {
                if (menubool("usecomboe"))
                    e.Cast(target.Position);

                if (target.Distance(player.Position) <= e.Range + w.Range + 100)
                {
                    if (menulist("emode") == 1)
                    {
                        if (canhd && hashd && !cb)
                        {
                            Items.UseItem(3077);
                            Items.UseItem(3074);
                        }

                        else
                        {
                            CheckR();
                        }
                    }

                    if (menulist("emode") == 0)
                    {
                        CheckR();
                    }
                }
            }

            else if (w.IsReady() && canw && menubool("usecombow") &&
                     target.Distance(player.Position) <= wrange)
            {
                useinventoryitems(target);
                CheckR();

                if (menulist("emode") == 0)
                {
                    if (menubool("usecombow"))
                        w.Cast();

                    if (canhd && hashd)
                    {
                        Items.UseItem(3077);
                        Items.UseItem(3074);
                    }
                }

                if (menulist("emode") == 1)
                {
                    if (canhd && hashd && !cb)
                    {
                        Items.UseItem(3077);
                        Items.UseItem(3074);
                        if (menubool("usecombow"))
                            Utility.DelayAction.Add(250, () => w.Cast());
                    }

                    else
                    {
                        CheckR();
                        if (menubool("usecombow"))
                            w.Cast();
                    }
                }
            }

            else if (q.IsReady() && target.Distance(player.Position) <= q.Range + 30)
            {
                useinventoryitems(target);
                CheckR();

                if (menulist("emode") == 0 || (ComboDamage(target) / 1.7) >= target.Health)
                {
                    if (Items.CanUseItem(3077) || Items.CanUseItem(3074))
                        return;
                }

                if (canq)
                    q.Cast(target.Position);
            }

            else if (target.Distance(player.Position) > truerange + 100)
            {
                if (menubool("usegap"))
                {
                    if (Utils.GameTimeTickCount - lastq >= menuslide("gaptimez") * 10)
                    {
                        if (q.IsReady() && Utils.GameTimeTickCount - laste >= 500)
                        {
                            q.Cast(target.Position);
                        }
                    }
                }
            }

        }

        #endregion

        #region Riven: Harass

        private static void HarassTarget(AIBaseClient target)
        {
            Vector3 qpos;
            switch (menulist("qtoo"))
            {
                case 0:
                    qpos = player.Position +
                        (player.Position - target.Position).Normalized() * 500;
                    break;
                case 1:
                    qpos = ObjectManager.Get<AITurretClient>()
                        .Where(t => (t.IsAlly)).OrderBy(t => t.Distance(player.Position)).First().Position;
                    break;
                default:
                    qpos = Game.CursorPosRaw;
                    break;
            }

            if (q.IsReady())
                OrbTo(target);

            if (cc >= 2 && canq)
            {
                canaa = false;
            }

            if (cc == 2 && canq && q.IsReady())
            {
                player.IssueOrder(GameObjectOrder.MoveTo, qpos);
                Utility.DelayAction.Add(200, () =>
                {
                    q.Cast(qpos);
                });
            }

            if (!player.Position.Extend(target.Position, q.Range * 3).UnderTurret(true))
            {
                if (q.IsReady() && canq && cc < 2)
                {
                    if (target.Distance(player.Position) <= truerange + q.Range)
                    {
                        q.Cast(target.Position);
                    }
                }
            }

            if (e.IsReady() && cane && q.IsReady() && cc < 1 &&
                target.Distance(player.Position) > truerange + 100 &&
                target.Distance(player.Position) <= e.Range + truerange + 50)
            {
                if (!player.Position.Extend(target.Position, e.Range).UnderTurret(true))
                {
                    if (menubool("usegaph"))
                        e.Cast(target.Position);
                }
            }

            else if (w.IsReady() && canw && target.Distance(player.Position) <= w.Range + 10)
            {
                if (!target.Position.UnderTurret(true))
                {
                    if (menubool("useharassw"))
                        w.Cast();
                }
            }

        }
        #endregion

        #region Riven: Windslash

        private static void Windslash()
        {
            if (uo && menubool("usews") && r.IsReady())
            {
                foreach (var t in ObjectManager.Get<AIHeroClient>().Where(h => h.IsValidTarget(r.Range)))
                {
                    if (r.GetDamage(t) >= t.Health && canws && !t.IsZombie)
                    {
                        if (r.GetPrediction(t, true).Hitchance == HitChance.VeryHigh)
                            r.Cast(r.GetPrediction(t, true).CastPosition);
                    }
                }

                if (menulist("wsmode") == 1 && rtarg.IsValidTarget(r.Range) && !rtarg.IsZombie)
                {
                    if (menu.Item("shycombo").GetValue<KeyBind>().Active && cb)
                        if (Items.CanUseItem(3077) || Items.CanUseItem(3074))
                            return;

                    var po = r.GetPrediction(rtarg, true);
                    var cx = 4 - cc;

                    if (Kappa(rtarg.Position, r.Width, r.Range) >= menuslide("rmulti"))
                    {
                        r.Cast(rtarg.Position);
                    }

                    if (r.GetDamage(rtarg) / rtarg.MaxHealth * 100 >= 55)
                    {
                        if (po.Hitchance >= HitChance.VeryHigh && canws)
                            r.Cast(po.CastPosition);
                    }

                    if (q.IsReady())
                    {
                        var cy = r.GetDamage(rtarg) +
                                player.GetAutoAttackDamage(rtarg) * 2 + Qdmg(rtarg) * cx;

                        if (rtarg.Health <= xtra((float)cy))
                        {
                            if (rtarg.Distance(player.Position) <= truerange + q.Range * cx)
                            {
                                if (po.Hitchance >= HitChance.VeryHigh && canws)
                                    r.Cast(po.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Lane/Jungle

        private static void Clear()
        {
            var minions = MinionManager.GetMinions(player.Position, 600f,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            foreach (var unit in minions.Where(m => !m.Name.Contains("Mini")))
            {
                OrbTo(unit);

                if (q.IsReady() && unit.Distance(player.Position) <= q.Range + 100)
                {
                    if (canq && menubool("usejungleq"))
                        q.Cast(unit.Position);
                }

                if (w.IsReady() && unit.Distance(player.Position) <= w.Range + 10)
                {
                    if (canw && menubool("usejunglew"))
                        w.Cast();
                }

                if (e.IsReady() && (unit.Distance(player.Position) > truerange + 30 ||
                    player.Health / player.MaxHealth * 100 <= 70))
                {
                    if (cane && menubool("usejunglee"))
                        e.Cast(unit.Position);
                }
            }
        }

        private static void Wave()
        {
            var minions = MinionManager.GetMinions(player.Position, 600f);

            foreach (var unit in minions)
            {
                if (player.GetAutoAttackDamage(unit, true) >= unit.Health)
                {
                    OrbTo(unit);
                }

                if (q.IsReady() && unit.Distance(player.Position) <= truerange + 100)
                {
                    if (canq && menubool("uselaneq") && minions.Count >= 2 &&
                       !player.Position.Extend(unit.Position, q.Range).UnderTurret(true))
                        q.Cast(unit.Position);
                }

                if (w.IsReady())
                {
                    if (minions.Count(m => m.Distance(player.Position) <= w.Range + 10) >= menuslide("wminion"))
                    {
                        if (canw && menubool("uselanew"))
                        {
                            Items.UseItem(3077);
                            Items.UseItem(3074);
                            w.Cast();
                        }
                    }
                }

                if (e.IsReady() && !player.Position.Extend(unit.Position, e.Range).UnderTurret(true))
                {
                    if (unit.Distance(player.Position) > truerange + 30)
                    {
                        if (cane && menubool("uselanee"))
                            e.Cast(unit.Position);
                    }

                    else if (player.Health / player.MaxHealth * 100 <= 70)
                    {
                        if (cane && menubool("uselanee"))
                            e.Cast(unit.Position);
                    }
                }
            }
        }

        #endregion

        #region Riven: Flee

        private static void Flee()
        {
            if (canmv)
            {
                player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosRaw);
            }

            if (ssfl)
            {
                if (Utils.GameTimeTickCount - lastq >= 600)
                {
                    q.Cast(Game.CursorPosRaw);
                }

                if (cane && e.IsReady())
                {
                    if (cc >= 2 || !q.IsReady() && !player.HasBuff("RivenTriCleave"))
                    {
                        if (!player.Position.Extend(Game.CursorPosRaw, e.Range + 10).IsWall())
                            e.Cast(Game.CursorPosRaw);
                    }
                }
            }

            else
            {
                if (q.IsReady())
                {
                    q.Cast(Game.CursorPosRaw);
                }

                if (e.IsReady() && Utils.GameTimeTickCount - lastq >= 250)
                {
                    if (!player.Position.Extend(Game.CursorPosRaw, e.Range).IsWall())
                        e.Cast(Game.CursorPosRaw);
                }
            }
        }

        #endregion



        #region Riven: Check R
        private static void CheckR()
        {
            if (!r.IsReady() || uo || !menubool("user"))
                return;

            if (menulist("ultwhen") == 2 && cc <= menuslide("userq"))
                r.Cast();

            var enemies = HeroManager.Enemies.Where(ene => ene.IsValidTarget(r.Range + 250));
            var targets = enemies as IList<AIHeroClient> ?? enemies.ToList();
            foreach (var target in targets)
            {
                if (cc <= menuslide("userq") && (q.IsReady() || Utils.GameTimeTickCount - lastq < 1000))
                {
                    if (targets.Count(ene => ene.Distance(player.Position) <= 650) >= 2)
                    {
                        r.Cast();
                    }

                    if (targets.Count() < 2 && target.Health / target.MaxHealth * 100 <= menuslide("overk"))
                    {
                        return;
                    }

                    if (menulist("ultwhen") == 0)
                    {
                        if ((ComboDamage(target) / 1.7) >= target.Health)
                        {
                            r.Cast();
                        }
                    }

                    // hard kill ->
                    if (menulist("ultwhen") == 1)
                    {
                        if (ComboDamage(target) >= target.Health)
                        {
                            r.Cast();
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: On Cast

        private static void OnCast()
        {
            AIBaseClient.OnProcessSpellCast += (sender, args) =>
            {
                if (sender.IsEnemy && sender.Type == player.Type && menubool("ashield"))
                {
                    var epos = player.Position +
                              (player.Position - sender.Position).Normalized() * 300;

                    if (player.Distance(sender.Position) <= args.SData.CastRange)
                    {
                        switch (args.SData.TargettingType)
                        {
                            case SpellDataTargetType.Unit:

                                if (args.Target.NetworkId == player.NetworkId)
                                {
                                    if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                                    {
                                        e.Cast(epos);
                                    }
                                }

                                break;
                            case SpellDataTargetType.SelfAoe:

                                if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                                {
                                    e.Cast(epos);
                                }

                                break;
                        }
                    }
                }

                if (!sender.IsMe)
                    return;

                switch (args.SData.Name)
                {
                    case "RivenTriCleave":
                        didq = true;
                        didaa = false;
                        canmv = false;
                        lastq = Utils.GameTimeTickCount;
                        canq = false;

                        if (!uo)
                            ssfl = false;

                        // cancel q animation
                        if (qtarg.IsValidTarget(q.Range + 250))
                        {
                            Utility.DelayAction.Add(100 + (100 - Game.Ping / 2),
                                () => player.IssueOrder(GameObjectOrder.MoveTo, movepos));
                        }

                        break;
                    case "RivenMartyr":
                        didw = true;
                        didaa = false;
                        lastw = Utils.GameTimeTickCount;
                        canw = false;

                        break;
                    case "RivenFeint":
                        dide = true;
                        didaa = false;
                        laste = Utils.GameTimeTickCount;
                        cane = false;

                        if (menu.Item("fleekey").GetValue<KeyBind>().Active)
                        {
                            if (uo && r.IsReady() && cc == 2 && q.IsReady())
                            {
                                r.Cast(Game.CursorPosRaw);
                            }
                        }

                        if (menu.Item("combokey").GetValue<KeyBind>().Active)
                        {
                            if (cc >= 2)
                            {
                                CheckR();
                                Utility.DelayAction.Add(Game.Ping + 100, () => q.Cast(Game.CursorPosRaw));
                            }
                        }

                        break;
                    case "RivenFengShuiEngine":
                        ssfl = true;

                        if (rtarg != null && cb)
                        {
                            if (!flash.IsReady() || !menubool("multib"))
                                return;

                            var ww = w.IsReady() ? w.Range + 10 : truerange;

                            if (menu.Item("shycombo").GetValue<KeyBind>().Active)
                            {
                                if (rtarg.Distance(player.Position) > e.Range + ww &&
                                    rtarg.Distance(player.Position) <= e.Range + ww + 300)
                                {
                                    player.Spellbook.CastSpell(flash, rtarg.Position);
                                }
                            }
                        }

                        break;
                    case "RivenIzunaBlade":
                        ssfl = false;
                        didws = true;
                        canws = false;

                        if (w.IsReady() && rtarg.IsValidTarget(wrange))
                            w.Cast();

                        if (q.IsReady() && rtarg.IsValidTarget())
                            q.Cast(rtarg.Position);

                        break;
                    case "ItemTiamatCleave":
                        lasthd = Utils.GameTimeTickCount;
                        didhd = true;
                        canws = true;
                        canhd = false;
                        canaa = true;

                        if (menulist("wsmode") == 1 && uo && canws)
                        {
                            if (menu.Item("combokey").GetValue<KeyBind>().Active ||
                                menu.Item("shycombo").GetValue<KeyBind>().Active)
                            {
                                if ((cb || menu.Item("shycombo").GetValue<KeyBind>().Active) &&
                                    r.GetPrediction(rtarg).Hitchance >= HitChance.Medium)
                                {
                                    if (rtarg.IsValidTarget() && !rtarg.IsZombie)
                                    {
                                        Utility.DelayAction.Add(150,
                                            () => r.Cast(r.GetPrediction(rtarg).CastPosition));
                                    }
                                }
                            }
                        }

                        if (menulist("emode") == 1 && menu.Item("combokey").GetValue<KeyBind>().Active)
                        {
                            CheckR();
                            Utility.DelayAction.Add(Game.Ping + 175, () => q.Cast(Game.CursorPosRaw));
                        }

                        break;
                }

                if (args.SData.Name.Contains("Attack"))
                {
                    if (menu.Item("combokey").GetValue<KeyBind>().Active ||
                        menu.Item("shycombo").GetValue<KeyBind>().Active)
                    {
                        if ((cb || menu.Item("shycombo").GetValue<KeyBind>().Active) ||
                            !menubool("usecombow") ||
                            !menubool("usecomboe"))
                        {
                            // delay till after aa
                            Utility.DelayAction.Add(
                                50 + (int)(player.AttackDelay * 100) + Game.Ping / 2 + menuslide("autoaq"), delegate
                                {
                                    if (Items.CanUseItem(3077))
                                        Items.UseItem(3077);
                                    if (Items.CanUseItem(3074))
                                        Items.UseItem(3074);
                                });
                        }
                    }

                    else if (menu.Item("clearkey").GetValue<KeyBind>().Active ||
                            (menu.Item("harasskey").GetValue<KeyBind>().Active && menubool("useitemh")))
                    {
                        if (qtarg.IsValid<AIBaseClient>() && !qtarg.Name.StartsWith("Minion"))
                        {
                            Utility.DelayAction.Add(
                                50 + (int)(player.AttackDelay * 100) + Game.Ping / 2 + menuslide("autoaq"), delegate
                                {
                                    if (Items.CanUseItem(3077))
                                        Items.UseItem(3077);
                                    if (Items.CanUseItem(3074))
                                        Items.UseItem(3074);
                                });
                        }
                    }
                }

                if (!didq && args.SData.Name.Contains("Attack"))
                {
                    didaa = true;
                    canaa = false;
                    canq = false;
                    canw = false;
                    cane = false;
                    canws = false;
                    lastaa = Utils.GameTimeTickCount;
                    qtarg = (AIBaseClient)args.Target;
                }
            };
        }

        #endregion

        #region Riven: Misc Events
        private static void Interrupter()
        {
            Interrupters.OnInterrupter += (interrupter) =>
            {
                var sender = interrupter.Sender;
                if (sender.IsEnemy)
                {
                    if (menubool("wint") && w.IsReady())
                    {
                        if (!sender.Position.UnderTurret(true))
                        {
                            if (sender.IsValidTarget(w.Range))
                                w.Cast();

                            if (sender.IsValidTarget(w.Range + e.Range) && e.IsReady())
                            {
                                e.Cast(sender.Position);
                            }
                        }
                    }

                    if (menubool("qint") && q.IsReady() && cc >= 2)
                    {
                        if (!sender.Position.UnderTurret(true))
                        {
                            if (sender.IsValidTarget(q.Range))
                                q.Cast(sender.Position);

                            if (sender.IsValidTarget(q.Range + e.Range) && e.IsReady())
                            {
                                e.Cast(sender.Position);
                            }
                        }
                    }
                }
            };
        }

        private static void OnGapcloser()
        {
            Gapclosers.OnGapcloser += gapcloser =>
            {
                if (gapcloser.Sender.IsEnemy)
                {
                    if (menubool("wgap") && w.IsReady())
                    {
                        if (gapcloser.Sender.IsValidTarget(w.Range))
                        {
                            if (!gapcloser.Sender.Position.UnderTurret(true))
                            {
                                w.Cast();
                            }
                        }

                    }

                    if (q.IsReady() && cc == 2)
                    {
                        if (gapcloser.Sender.IsValidTarget(q.Range) && !player.IsFacing(gapcloser.Sender))
                        {
                            if (Items.CanUseItem((int)Items.GetWardSlot().Id))
                            {
                                q.Cast(Game.CursorPosRaw);

                                if (didq)
                                    Items.UseItem((int)Items.GetWardSlot().Id, gapcloser.Sender.Position);
                            }
                        }
                    }
                }
            };
        }

        private void OnPlayAnimation()
        {
            AIBaseClient.OnPlayAnimation += (sender, args) =>
            {
                if (!(didq || didw || didws || dide) && !player.IsDead)
                {
                    if (sender.IsMe)
                    {
                        if (args.Animation.Contains("Idle"))
                        {
                            canq = false;
                            canaa = true;
                        }

                        if (args.Animation.Contains("Run"))
                        {
                            canq = false;
                            canaa = true;
                        }
                    }
                }

            };
        }

        #endregion

        #region Riven: Aura

        private static void AuraUpdate()
        {
            if (!player.IsDead)
            {
                foreach (var buff in player.Buffs)
                {
                    if (buff.Name == "RivenTriCleave")
                        cc = buff.Count;

                    if (buff.Name == "RivenPassiveAABoost")
                        pc = buff.Count;
                }

                if (player.HasBuff("RivenTriCleave"))
                {
                    if (Utils.GameTimeTickCount - lastq >= 3650)
                    {
                        if (!player.IsRecalling() && !player.Spellbook.IsChanneling)
                        {
                            var qext = player.Position.Extend(Game.CursorPosRaw, 400);

                            if (menubool("keepq") && !qext.UnderTurret(true))
                                q.Cast(Game.CursorPosRaw);
                        }
                    }
                }

                if (!player.HasBuff("RivenPassiveAABoost"))
                    Utility.DelayAction.Add(1000, () => pc = 1);

                if (!player.HasBuff("RivenTriCleave"))
                    Utility.DelayAction.Add(1000, () => cc = 0);
            }
        }

        #endregion

        #region Riven : Combat/Orbwalk

        private static bool IsOrbwalkKey(uint key)
        {
            if (menu.Item("Farm").GetValue<KeyBind>().Key == key ||
                menu.Item("Orbwalk").GetValue<KeyBind>().Key == key ||
                menu.Item("LaneClear").GetValue<KeyBind>().Key == key)
            {
                return true;
            }

            return false;
        }

        private static void OrbTo(AIBaseClient target, float rangeoverride = 0f)
        {
            if (canaa && canmv)
            {
                if (target.IsValidTarget(truerange + 50 + rangeoverride))
                {
                    if (IsOrbwalkKey(menu.Item("combokey").GetValue<KeyBind>().Key) &&
                        menu.Item("combokey").GetValue<KeyBind>().Active)
                    {
                        orbwalker.SetAttack(false);
                        orbwalker.SetMovement(false);
                    }

                    if (IsOrbwalkKey(menu.Item("harasskey").GetValue<KeyBind>().Key) &&
                        menu.Item("harasskey").GetValue<KeyBind>().Active)
                    {
                        orbwalker.SetAttack(false);
                        orbwalker.SetMovement(false);
                    }

                    if (IsOrbwalkKey(menu.Item("clearkey").GetValue<KeyBind>().Key) &&
                        menu.Item("clearkey").GetValue<KeyBind>().Active)
                    {
                        orbwalker.SetAttack(false);
                        orbwalker.SetMovement(false);
                    }

                    player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
        }

        private static void CombatCore()
        {
            if (didhd && canhd && Utils.GameTimeTickCount - lasthd >= 250)
            {
                didhd = false;
            }

            if (didq)
            {
                if (Utils.GameTimeTickCount - lastq >= (int)(player.AttackCastDelay * 1000) - Game.Ping / 2)
                {
                    didq = false;
                    canmv = true;
                    canaa = true;
                }
            }

            if (didw)
            {
                if (Utils.GameTimeTickCount - lastw >= 266)
                {
                    didw = false;
                    canmv = true;
                    canaa = true;
                }
            }

            if (dide)
            {
                if (Utils.GameTimeTickCount - laste >= 300)
                {
                    dide = false;
                    canmv = true;
                    canaa = true;
                }
            }

            if (didaa)
            {
                if (Utils.GameTimeTickCount - lastaa >= (player.AttackDelay * 100) + (100 - Game.Ping) + menuslide("autoaq"))
                {
                    didaa = false;
                    canmv = true;
                    canq = true;
                    cane = true;
                    canw = true;
                    canws = true;
                }
            }

            if (!canw && w.IsReady())
            {
                if (!(didaa || didq || dide))
                {
                    canw = true;
                }
            }

            if (!cane && e.IsReady())
            {
                if (!(didaa || didq || didw))
                {
                    cane = true;
                }
            }

            if (!canws && r.IsReady())
            {
                if (!(didaa || didw) && uo)
                {
                    canws = true;
                }
            }

            if (!canaa)
            {
                if (!(didq || didw || dide || didws || didhd || didhs))
                {
                    if (Utils.GameTimeTickCount - lastaa >= 1000)
                    {
                        canaa = true;
                    }
                }
            }

            if (!canmv)
            {
                if (!(didq || didw || dide || didws || didhd || didhs))
                {
                    if (Utils.GameTimeTickCount - lastaa >= 1100)
                    {
                        canmv = true;
                    }
                }
            }
        }

        #endregion

        #region Riven: Math/Damage

        private static float ComboDamage(AIBaseClient target)
        {
            if (target == null)
                return 0f;

            var ignote = player.GetSpellSlot("summonerdot");
            var ad = (float)player.GetAutoAttackDamage(target);
            var runicpassive = new[] { 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5 };

            var ra = ad +
                        (float)
                            ((+player.FlatPhysicalDamageMod + player.BaseAttackDamage) *
                            runicpassive[player.Level / 3]);

            var rw = Wdmg(target);
            var rq = Qdmg(target);
            var rr = r.IsReady() ? r.GetDamage(target) : 0;

            var ii = (ignote != SpellSlot.Unknown && player.GetSpell(ignote).State == SpellState.Ready && r.IsReady()
                ? player.GetSummonerSpellDamage(target, Damage.DamageSummonerSpell.Ignite)
                : 0);

            var tmt = Items.HasItem(3077) && Items.CanUseItem(3077)
                ? player.GetItemDamage(target, Damage.DamageItems.Tiamat)
                : 0;

            var hyd = Items.HasItem(3074) && Items.CanUseItem(3074)
                ? player.GetItemDamage(target, Damage.DamageItems.RavenousHydra)
                : 0;

            var th = Items.HasItem((int)ItemId.Titanic_Hydra) && Items.CanUseItem((int)ItemId.Titanic_Hydra)
                ? player.GetItemDamage(target, Damage.DamageItems.TitanicHydra)
                : 0;

            var bwc = Items.HasItem(3144) && Items.CanUseItem(3144)
                ? player.GetItemDamage(target, Damage.DamageItems.BilgewaterCutlass)
                : 0;

            var brk = Items.HasItem(3153) && Items.CanUseItem(3153)
                ? player.GetItemDamage(target, Damage.DamageItems.BotRK)
                : 0;

            var items = tmt + hyd + bwc + brk;

            var damage = (rq * 3 + ra * 3 + rw + rr + ii + th + items);

            return xtra((float)damage);
        }


        private static double Wdmg(AIBaseClient target)
        {
            double dmg = 0;
            if (w.IsReady() && target != null)
            {
                dmg += player.CalcDamage(target, Damage.DamageType.Physical,
                    new[] { 50, 80, 110, 150, 170 }[w.Level - 1] + 1 * player.FlatPhysicalDamageMod + player.BaseAttackDamage);
            }

            return dmg;
        }

        private static double Qdmg(AIBaseClient target)
        {
            double dmg = 0;
            if (q.IsReady() && target != null)
            {
                dmg += player.CalcDamage(target, Damage.DamageType.Physical,
                    -10 + (q.Level * 20) + (0.35 + (q.Level * 0.05)) * (player.FlatPhysicalDamageMod + player.BaseAttackDamage));
            }

            return dmg;
        }

        #endregion

        #region Riven: Drawings

        private static void Drawings()
        {
            Drawing.OnDraw += args =>
            {
                if (!player.IsDead)
                {
                    if (menubool("drawstatus"))
                    {
                        var mypos = Drawing.WorldToScreen(player.Position);
                        Drawing.DrawText(mypos[0] - 60, mypos[1] + 10, Color.White,
                            "Ult When: " + menu.Item("ultwhen").GetValue<StringList>().SelectedValue);
                    }

                    if (menubool("drawengage"))
                    {
                        Render.Circle.DrawCircle(player.Position,
                            player.AttackRange + e.Range + 35, Color.White, 2);
                    }

                    if (menubool("drawburst") && cb && flash.IsReady() && rtarg != null)
                    {
                        var ee = e.IsReady() ? e.Range : 0f;
                        var ww = w.IsReady() ? w.Range + 10 : truerange;
                        Render.Circle.DrawCircle(rtarg.Position, ee + ww + 300, Color.GreenYellow, 3);
                    }

                    if (menubool("drawtt") && rtarg != null)
                    {
                        Render.Circle.DrawCircle(rtarg.Position, e.Range, Color.White, 2);
                    }
                }
            };

            Drawing.OnEndScene += args =>
            {
                if (!menubool("drawdmg"))
                    return;

                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
                {
                    var color = r.IsReady() &&
                                (enemy.Health <= ComboDamage(enemy) / 1.6 ||
                                 enemy.CountEnemiesInRange(w.Range) >= menuslide("multic"))
                        ? new ColorBGRA(0, 255, 0, 90)
                        : new ColorBGRA(255, 255, 0, 90);

                    hpi.unit = enemy;
                    hpi.drawDmg(ComboDamage(enemy), color);
                }

            };
        }

        #endregion
    }

    internal class HpBarIndicatorRiven
    {
        public static Device dxDevice = Drawing.Direct3DDevice;
        public static Line dxLine;

        public float hight = 9;
        public float width = 104;


        public HpBarIndicatorRiven()
        {
            dxLine = new Line(dxDevice) { Width = 9 };

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        public AIHeroClient unit { get; set; }

        private Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public Vector2 startPosition
        {
            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            dxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            dxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            dxLine.OnLostDevice();
        }


        private float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public void drawDmg(float dmg, ColorBGRA color)
        {
            Vector2 hpPosNow = getHpPosAfterDmg(0);
            Vector2 hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
            //fillHPBar((int)(hpPosNow.X - startPosition.X), (int)(hpPosAfter.X- startPosition.X), color);
        }

        private void fillHPBar(int to, int from, Color color)
        {
            var sPos = startPosition;
            for (var i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private void fillHPBar(Vector2 from, Vector2 to, ColorBGRA color)
        {
            dxLine.Begin();

            dxLine.Draw(new[] {
                new Vector2((int) from.X, (int) from.Y + 4f),
                new Vector2((int) to.X, (int) to.Y + 4f) }, color);

            dxLine.End();
        }
    }
}

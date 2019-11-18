using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Utility = EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;

namespace RyusungAIO.Champions
{
    class Ryze
    {
        private const string ChampionName = "Ryze";

        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;

        private static Menu Config;

        private static AIHeroClient Player;


 

        public Ryze()
        {

            Notifications.Add(new Notification("Ryusung AIO fuck WWapper", "Ryze credit my brain"));
            Player = ObjectManager.Player;


            Q = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W, 615f);
            E = new Spell(SpellSlot.E, 615f);
            R = new Spell(SpellSlot.R, 1750f);
            Q.SetSkillshot(0.25f, 55f, 700f, true, false, SkillshotType.Line);

            Config = new Menu(ChampionName, "[RY]" + ChampionName, true);

            #region Combo
            Menu combo = new Menu("Combo", "Combo");
            combo.Add(new MenuBool("UseQOutRangeEW", "Use Q out range EW", false)).Permashow();
            combo.Add(new MenuBool("UseQCombo", "Use Q", true));
            combo.Add(new MenuBool("UseWCombo", "Use W"));
            combo.Add(new MenuBool("UseECombo", "Use E"));
            combo.Add(new MenuList("ComboPriority", "Combo Priority", new[] { "Q(Max Damage)", "W(Max stun)" })).Permashow();
            combo.Add(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press)).Permashow();
            Config.Add(combo);
            #endregion

            #region Misc
            Menu Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuBool("AutoW", "Auto W AntiGrapcloser"));
            Config.Add(Misc);
            #endregion

            #region Harass
            Menu Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuBool("UseQHarass", "Use Q"));
            Harass.Add(new MenuBool("UseWHarass", "Use W", false));
            Harass.Add(new MenuBool("UseEHarass", "Use E", false));
            Harass.Add(new MenuSlider("HarassManaCheck", "Don't harass if mana < %", 0, 0, 100));
            Harass.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press)).Permashow();
            Harass.Add(new MenuKeyBind("HarassActiveT", "Harass (toggle)!", Keys.Y, KeyBindType.Toggle)).Permashow();
            Config.Add(Harass);
            #endregion

            #region Farming
            Menu Farm = new Menu("Farm", "Farm");
            Farm.Add(new MenuBool("EnabledFarm", "Enable! (On/Off: Mouse Scroll)")).Permashow();
            Farm.Add(new MenuList("UseQFarm", "Use Q", new[] { "LastHit", "LaneClear", "Both", "No" }, 2));
            Farm.Add(new MenuList("UseWFarm", "Use W", new[] { "LastHit", "LaneClear", "Both", "No" }, 1));
            Farm.Add(new MenuList("UseEFarm", "Use E", new[] { "LastHit", "LaneClear", "Both", "No" }, 1));
            Farm.Add(new MenuSlider("LaneClearManaCheck", "Don't LaneClear if mana < %", 0, 0, 100));

            Farm.Add(new MenuKeyBind("LastHitActive", "LastHit!", Keys.X, KeyBindType.Press)).Permashow();
            Farm.Add(new MenuKeyBind("LaneClearActive", "LaneClear!", Keys.S, KeyBindType.Press)).Permashow();
            Config.Add(Farm);

            //JungleFarm menu:
            Menu JungleFarm = new Menu("JungleFarm", "JungleFarm");
            JungleFarm.Add(new MenuBool("UseQJFarm", "Use Q"));
            JungleFarm.Add(new MenuBool("UseWJFarm", "Use W"));
            JungleFarm.Add(new MenuBool("UseEJFarm", "Use E"));
            JungleFarm.Add(new MenuKeyBind("JungleFarmActive", "JungleFarm!", Keys.S, KeyBindType.Press)).Permashow();
            Config.Add(JungleFarm);
            #endregion

            #region Drawings
            Menu Drawings = new Menu("Drawings", "Drawings");
            //Drawings menu:
            Drawings.Add(new MenuBool("QRange", "Q range"));
            Drawings.Add(new MenuBool("WRange", "W range"));
            Drawings.Add(new MenuBool("ERange", "E range"));
            Config.Add(Drawings);

            #endregion

            Config.Attach();

            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
            //AIHeroClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;

            Game.Print("<font color=\"#FF9900\"><b>RY.Ryze</b></font> Author Sayuto");
            Game.Print("<font color=\"#FF9900\"><b>Feedback send to facebook yts.1996 </b></font>");
        }
        private static void Game_OnWndProc(GameWndProcEventArgs args)
        {
            if (args.Msg != 520)
                return;

            Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled = !Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled;
        }

        //static void AIBaseClientProcessSpellCast(AIBaseClient s, AIBaseClientProcessSpellCastEventArgs a)
        //{
        //    if (Config["Combo"].GetValue<MenuList>("ComboPriority").SelectedValue == "W(Max stun)")
        //    {
        //        return;
        //    }
        //    if(s == Player)
        //    {
        //        Game.Print("Total Time" + a.TotalTime);
        //        Game.Print("msspeed" + a.SData.MissileSpeed);
        //    }
        //}

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {

            if (!Config["Misc"].GetValue<MenuBool>("AutoW"))
                return;
            var attacker = sender;
            if (attacker.IsValidTarget(W.Range))
            {
                if (attacker.HasBuff("ryzee"))
                {
                    W.Cast(attacker, true);
                }
                else
                {
                    E.Cast(attacker, true);
                    W.Cast(attacker, true);
                }
            }
        }

        //private static void BestMinionE()
        //{
        //    var MinionInERange = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(E.Range)).OrderBy(m => m.DistanceToPlayer()).FirstOrDefault();
        //    var RangeEffectE = 350f;
        //    if (MinionInERange == null)
        //        return;
        //    var BestMinionForCast = GameObjects.EnemyMinions.Where(m =>
        //    {
        //        return m.IsValidTarget(E.Range) && m.IsValidTarget(RangeEffectE, true, MinionInERange.Position);
        //    });

        //}

        //private static bool IncludeNearestMinion(AIMinionClient minion, AIMinionClient nearestMinion)
        //{
        //    return minion.Distance(nearestMinion) < 350f;
        //}

        //private static int CountMinionsHitEffectE(AIMinionClient minion, IEnumerable<AIMinionClient> AllMinions)
        //{
        //    return AllMinions.Where(m => m.Distance(minion.Position) < 350f).Count();
        //}

        private static void Farm(bool laneClear)
        {
            if (!Config["Farm"].GetValue<MenuBool>("EnabledFarm"))
            {
                return;
            }

            var useQi = Config["Farm"].GetValue<MenuList>("UseQFarm").SelectedValue;
            var useWi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;
            var useEi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;

            var useQ = (laneClear && (useQi == "LaneClear" || useQi == "Both")) || (!laneClear && (useQi == "LastHit" || useQi == "Both"));
            var useW = (laneClear && (useWi == "LaneClear" || useWi == "Both")) || (!laneClear && (useWi == "LastHit" || useWi == "Both"));
            var useE = (laneClear && (useEi == "LaneClear" || useEi == "Both")) || (!laneClear && (useEi == "LastHit" || useEi == "Both"));

            if (laneClear)
            {
                var allMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range)).OrderBy(m => m.Health / m.MaxHealth * 100).Cast<AIBaseClient>().ToList();
                var MinionLeastHp = allMinions.FirstOrDefault();
                if (MinionLeastHp != null && useQ && Q.IsReady() && Player.Mana > 500)
                {
                    var QCanHit = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range)).OrderBy(m => m.Health / m.MaxHealth * 100).Cast<AIBaseClient>().ToList();
                    if (QCanHit.FirstOrDefault() != null)
                        Q.Cast(QCanHit.FirstOrDefault(), true);
                }
                if (useE && E.IsReady())
                {
                    E.Cast(MinionLeastHp, true);
                }
                else if (useQ && Q.IsReady())
                {
                    var MinionsHasEBuff = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.HasBuff("ryzee")).OrderBy(m => m.Distance(Player)).Cast<AIBaseClient>().ToList();
                    var MinionHasEBuffNearst = MinionsHasEBuff.FirstOrDefault();
                    if(MinionHasEBuffNearst != null){
                    
                        Q.Cast(MinionHasEBuffNearst, true);
                        }
                }
                else if (useW && W.IsReady())
                {
                    W.Cast(MinionLeastHp, true);
                }
            }
            else
            {
                var minionsWE = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range)).OrderBy(m => m.Health).Cast<AIBaseClient>().ToList();

                if (useQ && Q.IsReady())
                {
                    var minionsCanKill = minionsWE.Where(m => Q.GetDamage(m) >= m.Health).Cast<AIBaseClient>().ToList().FirstOrDefault();
                    if (minionsCanKill != null)
                        Q.Cast(minionsCanKill, true);
                }
                if (useE && E.IsReady())
                {
                    var minionsCanKill = minionsWE.Where(m => E.GetDamage(m) >= m.Health).Cast<AIBaseClient>().ToList().FirstOrDefault();
                    if (minionsCanKill != null)
                        E.Cast(minionsCanKill, true);
                }
                if (useW && W.IsReady())
                {
                    var minionsCanKill = minionsWE.Where(m => W.GetDamage(m) >= m.Health).Cast<AIBaseClient>().ToList().FirstOrDefault();
                    if (minionsCanKill != null)
                        W.Cast(minionsCanKill, true);
                }
            }
        }

        private static void JungleFarm(List<AIBaseClient> mobs)
        {
            var useQ = Config["JungleFarm"].GetValue<MenuBool>("UseQJFarm");
            var useW = Config["JungleFarm"].GetValue<MenuBool>("UseWJFarm");
            var useE = Config["JungleFarm"].GetValue<MenuBool>("UseEJFarm");

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useE && E.IsReady())
                {
                    E.Cast(mob, true);
                }
                if (useQ && Q.IsReady())
                {
                    Q.Cast(mob, true);
                }
                if (useW && W.IsReady() && (Player.Mana >= Q.Mana + W.Mana && !Q.IsReady()))
                {
                    W.Cast(mob, true);
                }
            }
        }

        static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range);
            if (Config["Combo"].GetValue<MenuBool>("UseQOutRangeEW"))
            {
                target = TargetSelector.GetTarget(Q.Range);
            }
            if (target == null)
            {
                return;
            }

            var useQ = Config["Combo"].GetValue<MenuBool>("UseQCombo");
            var useW = Config["Combo"].GetValue<MenuBool>("UseWCombo");
            var useE = Config["Combo"].GetValue<MenuBool>("UseECombo");


            switch (Config["Combo"].GetValue<MenuList>("ComboPriority").SelectedValue)
            {
                case "Q(Max Damage)": // q  - e - q - w - 
                    if (Player.Mana >= Q.Mana * 3 + E.Mana + W.Mana)
                    {
                        if (useQ && Q.IsReady())
                        {
                            Q.Cast(target, true);
                        }
                        if (useE && E.IsReady() && !Q.IsReady())
                        {
                            E.Cast(target, true);
                        }
                        if (useW && W.IsReady() && !Q.IsReady() && !E.IsReady())
                        {
                            if (target.HasBuff("ryzee"))
                                W.Cast(target, true);
                        }
                    }
                    else if (Player.Mana >= Q.Mana * 2 + E.Mana)
                    {
                        if (useQ && Q.IsReady())
                        {
                            Q.Cast(target, true);
                        }
                        if (useE && E.IsReady() && !Q.IsReady())
                        {
                            E.Cast(target, true);
                        }

                    }
                    else if (Player.Mana >= Q.Mana + E.Mana)
                    {
                        if (useE && E.IsReady())
                        {
                            E.Cast(target, true);
                        }
                        if (useQ && Q.IsReady())
                        {
                            Q.Cast(target, true);
                        }

                    }
                    else
                    {
                        Q.Cast(target, true);
                        E.Cast(target, true);
                    }
                    break;
                case "W(Max stun)": // q - e - w - q
                    if (Player.Mana >= Q.Mana * 2 + W.Mana + E.Mana || Player.Mana >= Q.Mana + W.Mana + E.Mana)
                    {
                        if (useW && W.IsReady() && target.HasBuff("ryzee"))
                        {
                            W.Cast(target, true);
                        }
                        if (useE && E.IsReady())
                        {
                            E.Cast(target, true);
                            DelayAction.Add((int)(target.Distance(Player.Position) / 3.5f + Game.Ping), () => {
                                W.Cast(target, true);
                            });
                        }
                        if (useQ && Q.IsReady() && (!target.HasBuff("ryzee") || !W.IsReady()))
                        {
                            Q.Cast(target, true);
                        }

                    }
                    else if (Player.Mana >= +W.Mana + E.Mana)
                    {
                        if (useW && W.IsReady() && target.HasBuff("ryzee"))
                        {
                            W.Cast(target, true);
                        }
                        if (useE && E.IsReady())
                        {
                            E.Cast(target, true);
                        }

                    }
                    else
                    {
                        W.Cast(target, true);
                    }
                    break;
            }

        }

        static void Harass()
        {
            if (Player.ManaPercent < Config["Harass"].GetValue<MenuSlider>("HarassManaCheck").Value)
                return;

            var targetQ = TargetSelector.GetTargets(Q.Range).Where(t => t.IsValidTarget(Q.Range)).OrderBy(x => 1 / x.Health).FirstOrDefault();
            var targetE = TargetSelector.GetTarget(E.Range);
            if (targetQ != null || targetE != null)
            {
                if (Config["Harass"].GetValue<MenuBool>("UseQHarass") && Q.IsReady() && targetQ != null)
                {
                    Q.Cast(targetQ, true);
                }
                if (Config["Harass"].GetValue<MenuBool>("UseEHarass") && E.IsReady() && targetE != null)
                {
                    E.Cast(targetE);
                }
                if (Config["Harass"].GetValue<MenuBool>("UseWHarass") && W.IsReady() && targetE != null)
                {
                    W.Cast(targetE);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {

            if (Player.IsDead)
            {
                return;
            }



            //var autoWminTargets = Config["Misc"].GetValue<MenuBool>("AutoW");


            if (Config["Combo"].GetValue<MenuKeyBind>("ComboActive").Active)
            {
                Combo();
            }
            else
            {
                if (Config["Harass"].GetValue<MenuKeyBind>("HarassActive").Active ||
                    (Config["Harass"].GetValue<MenuKeyBind>("HarassActiveT").Active && !Player.HasBuff("Recall")))
                {
                    Harass();
                }
                else
                {
                    var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth).Cast<AIBaseClient>().ToList();
                    if (mobs.Count == 0)
                    {
                        var lc = Config["Farm"].GetValue<MenuKeyBind>("LaneClearActive").Active;
                        if (lc || Config["Farm"].GetValue<MenuKeyBind>("LastHitActive").Active)
                        {
                            Farm(lc && (Player.Mana * 100 / Player.MaxMana >= Config["Farm"].GetValue<MenuSlider>("LaneClearManaCheck").Value));
                        }
                    }
                    else
                    {
                        if (Config["JungleFarm"].GetValue<MenuKeyBind>("JungleFarmActive").Active)
                        {
                            JungleFarm(mobs);
                        }
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var qCircle = Config["Drawings"].GetValue<MenuBool>("QRange");
            if (Config["Drawings"].GetValue<MenuBool>("QRange").Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("WRange").Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("ERange").Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.FromArgb(150, Color.DodgerBlue));
            }
        }


    }
}

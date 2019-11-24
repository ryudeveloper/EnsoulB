﻿using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using Keys = System.Windows.Forms.Keys;
using SPrediction;
using RyusungAIO.Helpers;

namespace RyusungAIO.Champions
{
    public class Viktor
    {
        
        private const string CHAMP_NAME = "Viktor";
        private static readonly AIHeroClient player = ObjectManager.Player;

        public static List<Spell> SpellList = new List<Spell>();
        // Spells
        private static Spell Q, W, E, R;
        private static readonly int maxRangeE = 1225;
        private static readonly int lengthE = 700;
        private static readonly int speedE = 1050;
        private static readonly int rangeE = 525;
        private static int lasttick = 0;
        private static SharpDX.Vector3 GapCloserPos;
        private static bool AttacksEnabled
        {
            get
            {
                if (keyLinks["comboActive"].GetValue<MenuKeyBind>().Active)
                {
                    return ((!Q.IsReady() || player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || player.Mana < E.Instance.ManaCost) && (!boolLinks["qAuto"].GetValue<MenuBool>() || player.HasBuff("viktorpowertransferreturn")));
                }
                else if (keyLinks["harassActive"].GetValue<MenuKeyBind>().Active)
                {
                    return ((!Q.IsReady() || player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || player.Mana < E.Instance.ManaCost));
                }
                return true;
            }
        }
        // Menu
        public static Menu menu;

        // Menu links
        public static Dictionary<string, MenuBool> boolLinks = new Dictionary<string, MenuBool>();
        public static Dictionary<string, MenuColor> circleLinks = new Dictionary<string, MenuColor>();
        public static Dictionary<string, MenuKeyBind> keyLinks = new Dictionary<string, MenuKeyBind>();
        public static Dictionary<string, MenuSlider> sliderLinks = new Dictionary<string, MenuSlider>();
        public static Dictionary<string, MenuList> stringLinks = new Dictionary<string, MenuList>();


        private static void OrbwalkerOnBeforeAttack(
    Object sender,
    OrbwalkerActionArgs args
)
        {
            if (args.Type == OrbwalkerType.BeforeAttack) {
                if (args.Target.Type == GameObjectType.AIHeroClient)
                {
                    args.Process = AttacksEnabled;
                }
                else
                    args.Process = true;
            }
            if(args.Type == OrbwalkerType.NonKillableMinion)
            {
                QLastHit((AIBaseClient)args.Target);
            }
            

        }
        public Viktor()
        {
            // Champ validation
          



            // Define spells
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, rangeE);
            R = new Spell(SpellSlot.R, 700);
            Spell Emax = new Spell(SpellSlot.E, 1025);
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(Emax);
            // Finetune spells
            Q.SetTargetted(0.25f, 2000);
            W.SetSkillshot(0.5f, 300, float.MaxValue, false, SkillshotType.Circle);
            E.SetSkillshot(0, 80, speedE, false, SkillshotType.Line);
            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.Circle);

            // Create menu
            SetupMenu();

            // Register events
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalker.OnAction += OrbwalkerOnBeforeAttack;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
        }
  
        private static void QLastHit(AIBaseClient minion)
        {
            bool castQ = ((keyLinks["waveUseQLH"].GetValue<MenuKeyBind>().Active) || boolLinks["waveUseQ"].GetValue<MenuBool>() && keyLinks["waveActive"].GetValue<MenuKeyBind>().Active);
            if (castQ)
            {
                var distance = player.Distance(minion);
                var t = 250 + (int)distance / 2;
                var preRyusungealth = HealthPrediction.GetPrediction(minion, t, 0);
                // Console.WriteLine(" Distance: " + distance + " timer : " + t + " health: " + preRyusungealth);
                if (preRyusungealth > 0 && Q.IsKillable(minion))
                {
                    Q.Cast(minion);
                }
            }
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (keyLinks["comboActive"].GetValue<MenuKeyBind>().Active)
                OnCombo();
            // Harass�
            if (keyLinks["harassActive"].GetValue<MenuKeyBind>().Active)
                OnHarass();
            // WaveClear
            if (keyLinks["waveActive"].GetValue<MenuKeyBind>().Active)
                OnWaveClear();

            if (keyLinks["jungleActive"].GetValue<MenuKeyBind>().Active)
                OnJungleClear();

            if (keyLinks["FleeActive"].GetValue<MenuKeyBind>().Active)
                Flee();

            if (keyLinks["forceR"].GetValue<MenuKeyBind>().Active)
            {
                if (R.IsReady())
                {
                    List<AIHeroClient> ignoredchamps = new List<AIHeroClient>();

                    foreach (var hero in HeroManager.Enemies)
                    {
                        if (!boolLinks["RU" + hero.CharacterName].GetValue<MenuBool>())
                        {
                            ignoredchamps.Add(hero);
                        }
                    }
                    AIHeroClient RTarget = TargetSelector.GetTarget(R.Range);
                    if (RTarget.IsValidTarget())
                    {
                        R.Cast(RTarget);
                    }
                }

            }
            // Ultimate follow
            if (R.Instance.Name != "ViktorChaosStorm" && boolLinks["AutoFollowR"].GetValue<MenuBool>() && Environment.TickCount - lasttick > 0)
            {
                var stormT = TargetSelector.GetTarget(1100);
                if (stormT != null)
                {
                    R.Cast(stormT.Position);
                    lasttick = Environment.TickCount + 500;
                }
            }
        }

        private void OnCombo()
        {

            try
            {


                bool useQ = boolLinks["comboUseQ"].GetValue<MenuBool>() && Q.IsReady();
                bool useW = boolLinks["comboUseW"].GetValue<MenuBool>() && W.IsReady();
                bool useE = boolLinks["comboUseE"].GetValue<MenuBool>() && E.IsReady();
                bool useR = boolLinks["comboUseR"].GetValue<MenuBool>() && R.IsReady();

                bool killpriority = boolLinks["spPriority"].GetValue<MenuBool>() && R.IsReady();
                bool rKillSteal = boolLinks["rLastHit"].GetValue<MenuBool>();
                AIHeroClient Etarget = TargetSelector.GetTarget(maxRangeE);
                AIHeroClient Qtarget = TargetSelector.GetTarget(Q.Range);
                AIHeroClient RTarget = TargetSelector.GetTarget(R.Range);
                if (killpriority && Qtarget != null & Etarget != null && Etarget != Qtarget && ((Etarget.Health > TotalDmg(Etarget, false, true, false, false)) || (Etarget.Health > TotalDmg(Etarget, false, true, true, false) && Etarget == RTarget)) && Qtarget.Health < TotalDmg(Qtarget, true, true, false, false))
                {
                    Etarget = Qtarget;
                }

                if (RTarget != null && rKillSteal && useR && boolLinks["RU" + RTarget.CharacterName].GetValue<MenuBool>())
                {
                    if (TotalDmg(RTarget, true, true, false, false) < RTarget.Health && TotalDmg(RTarget, true, true, true, true) > RTarget.Health)
                    {
                        R.Cast(RTarget.Position);
                    }
                }


                if (useE)
                {
                    if (Etarget != null)
                        PredictCastE(Etarget);
                }
                if (useQ)
                {

                    if (Qtarget != null)
                        Q.Cast(Qtarget);
                }
                if (useW)
                {
                    var t = TargetSelector.GetTarget(W.Range);

                    if (t != null)
                    {
                        if (t.Path.Count() < 2)
                        {
                            if (t.HasBuffOfType(BuffType.Slow))
                            {
                                if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                    if (W.Cast(t) == CastStates.SuccessfullyCasted)
                                        return;
                            }
                            if (t.CountEnemiesInRange(250) > 2)
                            {
                                if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                    if (W.Cast(t) == CastStates.SuccessfullyCasted)
                                        return;
                            }
                        }
                    }
                }
                if (useR && R.Instance.Name == "ViktorChaosStorm" && player.CanCast && !player.Spellbook.IsCastingSpell)
                {

                    foreach (var unit in HeroManager.Enemies.Where(h => h.IsValidTarget(R.Range)))
                    {
                        R.CastIfWillHit(unit, Array.IndexOf(stringLinks["HitR"].GetValue<MenuList>().Items, stringLinks["HitR"].GetValue<MenuList>().SelectedValue) + 1);

                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
        }

        private static void Flee()
        {
            Orbwalker.Move(Game.CursorPos);
            if (!Q.IsReady() || !(player.HasBuff("viktorqaug") || player.HasBuff("viktorqeaug") || player.HasBuff("viktorqwaug") || player.HasBuff("viktorqweaug")))
            {
                return;
            }
            var closestminion = GameObjects.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy).MinOrDefault(m => player.Distance(m));
            var closesthero = HeroManager.Enemies.MinOrDefault(m => player.Distance(m) < Q.Range);
            if (closestminion.IsValidTarget(Q.Range))
            {
                Q.Cast(closestminion);
            }
            else if (closesthero.IsValidTarget(Q.Range))
            {
                Q.Cast(closesthero);

            }
        }


        private static void OnHarass()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["harassMana"].GetValue<MenuSlider>().Value)
                return;
            bool useE = boolLinks["harassUseE"].GetValue<MenuBool>() && E.IsReady();
            bool useQ = boolLinks["harassUseQ"].GetValue<MenuBool>() && Q.IsReady();
            if (useQ)
            {
                var qtarget = TargetSelector.GetTarget(Q.Range);
                if (qtarget != null)
                    Q.Cast(qtarget);
            }
            if (useE)
            {
                var harassrange = sliderLinks["eDistance"].GetValue<MenuSlider>().Value;
                var target = TargetSelector.GetTarget(harassrange);

                if (target != null)
                    PredictCastE(target);
            }
        }

        private static void OnWaveClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["waveMana"].GetValue<MenuSlider>().Value)
                return;

            bool useQ = boolLinks["waveUseQ"].GetValue<MenuBool>() && Q.IsReady();
            bool useE = boolLinks["waveUseE"].GetValue<MenuBool>() && E.IsReady();

            if (useQ)
            {
                foreach (var minion in GameObjects.GetMinions(player.Position, player.AttackRange))
                {
                    if (Q.IsKillable(minion) && minion.CharacterName.Contains("Siege"))
                    {
                        QLastHit(minion);
                        break;
                    }
                }
            }

            if (useE)
                PredictCastMinionE();
        }

        private static void OnJungleClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["waveMana"].GetValue<MenuSlider>().Value)
                return;

            bool useQ = boolLinks["waveUseQ"].GetValue<MenuBool>() && Q.IsReady();
            bool useE = boolLinks["waveUseE"].GetValue<MenuBool>() && E.IsReady();

            if (useQ)
            {
                foreach (var minion in GameObjects.Jungle.Where(x => x.IsValidTarget(player.AttackRange)).OrderBy(x => x.MaxHealth).ToList())
                {
                    Q.Cast(minion);
                }
            }

            if (useE)
                PredictCastMinionEJungle();
        }

        public static FarmLocation GetBestLaserFarmLocation(bool jungle)
        {
            var bestendpos = new SharpDX.Vector2();
            var beststartpos = new SharpDX.Vector2();
            var minionCount = 0;
            List<AIBaseClient> allminions;
            var minimalhit = sliderLinks["waveNumE"].GetValue<MenuSlider>().Value;
            if (!jungle)
            {
                allminions = GameObjects.GetMinions(maxRangeE);

            }
            else
            {
                allminions = GameObjects.Jungle.Where(x => x.IsValidTarget(maxRangeE)).ToList<AIBaseClient>();
            }
            var minionslist = (from mnion in allminions select mnion.Position.ToVector2()).ToList<SharpDX.Vector2>();
            var posiblePositions = new List<SharpDX.Vector2>();
            posiblePositions.AddRange(minionslist);
            var max = posiblePositions.Count;
            for (var i = 0; i < max; i++)
            {
                for (var j = 0; j < max; j++)
                {
                    if (posiblePositions[j] != posiblePositions[i])
                    {
                        posiblePositions.Add((posiblePositions[j] + posiblePositions[i]) / 2);
                    }
                }
            }

            foreach (var startposminion in allminions.Where(m => player.Distance(m) < rangeE))
            {
                var startPos = startposminion.Position.ToVector2();

                foreach (var pos in posiblePositions)
                {
                    if (pos.Distance(startPos) <= lengthE * lengthE)
                    {
                        var endPos = startPos + lengthE * (pos - startPos).Normalized();

                        var count =
                            minionslist.Count(pos2 => pos2.Distance(startPos, endPos, true) <= 140 * 140);

                        if (count >= minionCount)
                        {
                            bestendpos = endPos;
                            minionCount = count;
                            beststartpos = startPos;
                        }

                    }
                }
            }
            if ((!jungle && minimalhit < minionCount) || (jungle && minionCount > 0))
            {
                //Console.WriteLine("MinimalHits: " + minimalhit + "\n Startpos: " + beststartpos + "\n Count : " + minionCount);
                return new FarmLocation(beststartpos, bestendpos, minionCount);
            }
            else
            {
                return new FarmLocation(beststartpos, bestendpos, 0);
            }
        }



        private static bool PredictCastMinionEJungle()
        {
            var farmLocation = GetBestLaserFarmLocation(true);

            if (farmLocation.MinionsHit > 0)
            {
                CastE(farmLocation.Position1, farmLocation.Position2);
                return true;
            }

            return false;
        }

        public struct FarmLocation
        {
            /// <summary>
            /// The minions hit
            /// </summary>
            public int MinionsHit;

            /// <summary>
            /// The start position
            /// </summary>
            public SharpDX.Vector2 Position1;


            /// <summary>
            /// The end position
            /// </summary>
            public SharpDX.Vector2 Position2;

            /// <summary>
            /// Initializes a new instance of the <see cref="FarmLocation"/> struct.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="minionsHit">The minions hit.</param>
            public FarmLocation(SharpDX.Vector2 startpos, SharpDX.Vector2 endpos, int minionsHit)
            {
                Position1 = startpos;
                Position2 = endpos;
                MinionsHit = minionsHit;
            }
        }
        private static bool PredictCastMinionE()
        {
            var farmLoc = GetBestLaserFarmLocation(false);
            if (farmLoc.MinionsHit > 0)
            {
                Console.WriteLine("Minion amount: " + farmLoc.MinionsHit + "\n Startpos: " + farmLoc.Position1 + "\n EndPos: " + farmLoc.Position2);

                CastE(farmLoc.Position1, farmLoc.Position2);
                return true;
            }

            return false;
        }


        private static void PredictCastE(AIHeroClient target)
        {
            // Helpers
            bool inRange = SharpDX.Vector2.DistanceSquared(target.Position.ToVector2(), player.Position.ToVector2()) < E.Range * E.Range;
            SpellPrediction.PredictionOutput prediction;
            bool spellCasted = false;

            // Positions
            SharpDX.Vector3 pos1, pos2;

            // Champs
            var nearChamps = (from champ in ObjectManager.Get<AIHeroClient>() where champ.IsValidTarget(maxRangeE) && target != champ select champ).ToList();
            var innerChamps = new List<AIHeroClient>();
            var outerChamps = new List<AIHeroClient>();
            foreach (var champ in nearChamps)
            {
                if (SharpDX.Vector2.DistanceSquared(champ.Position.ToVector2(), player.Position.ToVector2()) < E.Range * E.Range)
                    innerChamps.Add(champ);
                else
                    outerChamps.Add(champ);
            }

            // Minions
            var nearMinions = GameObjects.GetMinions(player.Position, maxRangeE);
            var innerMinions = new List<AIBaseClient>();
            var outerMinions = new List<AIBaseClient>();
            foreach (var minion in nearMinions)
            {
                if (SharpDX.Vector2.DistanceSquared(minion.Position.ToVector2(), player.Position.ToVector2()) < E.Range * E.Range)
                    innerMinions.Add(minion);
                else
                    outerMinions.Add(minion);
            }

            // Main target in close range
            if (inRange)
            {
                // Get prediction reduced speed, adjusted sourcePosition
                E.Speed = speedE * 0.9f;
                E.From = target.Position + (SharpDX.Vector3.Normalize(player.Position - target.Position) * (lengthE * 0.1f));
                prediction = E.GetPrediction(target);
                E.From = player.Position;

                // Prediction in range, go on
                if (prediction.CastPosition.Distance(player.Position) < E.Range)
                    pos1 = prediction.CastPosition;
                // Prediction not in range, use exact position
                else
                {
                    pos1 = target.Position;
                    E.Speed = speedE;
                }

                // Set new sourcePosition
                E.From = pos1;
                E.RangeCheckFrom = pos1;

                // Set new range
                E.Range = lengthE;

                // Get next target
                if (nearChamps.Count > 0)
                {
                    // Get best champion around
                    var closeToPrediction = new List<AIHeroClient>();
                    foreach (var enemy in nearChamps)
                    {
                        // Get prediction
                        prediction = E.GetPrediction(enemy);
                        // Validate target
                        if (prediction.Hitchance >= HitChance.High && SharpDX.Vector2.DistanceSquared(pos1.ToVector2(), prediction.CastPosition.ToVector2()) < (E.Range * E.Range) * 0.8)
                            closeToPrediction.Add(enemy);
                    }

                    // Champ found
                    if (closeToPrediction.Count > 0)
                    {
                        // Sort table by health DEC
                        if (closeToPrediction.Count > 1)
                            closeToPrediction.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                        // Set destination
                        prediction = E.GetPrediction(closeToPrediction[0]);
                        pos2 = prediction.CastPosition;

                        // Cast spell
                        CastE(pos1, pos2);
                        spellCasted = true;
                    }
                }

                // Spell not casted
                if (!spellCasted)
                {
                    CastE(pos1, E.GetPrediction(target).CastPosition);
                }

                // Reset spell
                E.Speed = speedE;
                E.Range = rangeE;
                E.From = player.Position;
                E.RangeCheckFrom = player.Position;
            }

            // Main target in extended range
            else
            {
                // Radius of the start point to search enemies in
                float startPointRadius = 150;

                // Get initial start point at the border of cast radius
                SharpDX.Vector3 startPoint = player.Position + SharpDX.Vector3.Normalize(target.Position - player.Position) * rangeE;

                // Potential start from postitions
                var targets = (from champ in nearChamps where SharpDX.Vector2.DistanceSquared(champ.Position.ToVector2(), startPoint.ToVector2()) < startPointRadius * startPointRadius && SharpDX.Vector2.DistanceSquared(player.Position.ToVector2(), champ.Position.ToVector2()) < rangeE * rangeE select champ).ToList();
                if (targets.Count > 0)
                {
                    // Sort table by health DEC
                    if (targets.Count > 1)
                        targets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                    // Set target
                    pos1 = targets[0].Position;
                }
                else
                {
                    var minionTargets = (from minion in nearMinions where SharpDX.Vector2.DistanceSquared(minion.Position.ToVector2(), startPoint.ToVector2()) < startPointRadius * startPointRadius && SharpDX.Vector2.DistanceSquared(player.Position.ToVector2(), minion.Position.ToVector2()) < rangeE * rangeE select minion).ToList();
                    if (minionTargets.Count > 0)
                    {
                        // Sort table by health DEC
                        if (minionTargets.Count > 1)
                            minionTargets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                        // Set target
                        pos1 = minionTargets[0].Position;
                    }
                    else
                        // Just the regular, calculated start pos
                        pos1 = startPoint;
                }

                // Predict target position
                E.From = pos1;
                E.Range = lengthE;
                E.RangeCheckFrom = pos1;
                prediction = E.GetPrediction(target);

                // Cast the E
                if (prediction.Hitchance >= HitChance.High)
                    CastE(pos1, prediction.CastPosition);

                // Reset spell
                E.Range = rangeE;
                E.From = player.Position;
                E.RangeCheckFrom = player.Position;
            }

        }



        private static void CastE(SharpDX.Vector3 source, SharpDX.Vector3 destination)
        {
            E.Cast(source, destination);
        }

        private static void CastE(SharpDX.Vector2 source, SharpDX.Vector2 destination)
        {
            E.Cast(source, destination);
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            var unit = args.Sender;
            if (args.DangerLevel >= Interrupter.DangerLevel.High && unit.IsEnemy)
            {
                var useW = boolLinks["wInterrupt"].GetValue<MenuBool>();
                var useR = boolLinks["rInterrupt"].GetValue<MenuBool>();

                if (useW && W.IsReady() && unit.IsValidTarget(W.Range) &&
                    (Game.Time + 1.5 + W.Delay) >= args.EndTime)
                {
                    if (W.Cast(unit) == CastStates.SuccessfullyCasted)
                        return;
                }
                else if (useR && unit.IsValidTarget(R.Range) && R.Instance.Name == "ViktorChaosStorm")
                {
                    R.Cast(unit);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(
    AIHeroClient sender,
    Gapcloser.GapcloserArgs args
)
        {
            if (sender.IsAlly)
            {
                return;
            }
            if (boolLinks["miscGapcloser"].GetValue<MenuBool>() && W.IsInRange(args.EndPosition) && sender.IsEnemy && args.EndPosition.DistanceToPlayer() < 200)
            {
                GapCloserPos = args.EndPosition;
                if (args.StartPosition.Distance(args.EndPosition) > sender.Spellbook.GetSpell(args.Slot).SData.CastRangeDisplayOverride && sender.Spellbook.GetSpell(args.Slot).SData.CastRangeDisplayOverride > 100)
                {
                    GapCloserPos = args.StartPosition.Extend(args.EndPosition, sender.Spellbook.GetSpell(args.Slot).SData.CastRangeDisplayOverride);
                }
                W.Cast(GapCloserPos.ToVector2(), true);
            }
        }
        private static void AutoW()
        {
            if (!W.IsReady() || !boolLinks["autoW"].GetValue<MenuBool>())
                return;

            var tPanth = HeroManager.Enemies.Find(h => h.IsValidTarget(W.Range) && h.HasBuff("Pantheon_GrandSkyfall_Jump"));
            if (tPanth != null)
            {
                if (W.Cast(tPanth) == CastStates.SuccessfullyCasted)
                    return;
            }

            foreach (var enemy in HeroManager.Enemies.Where(h => h.IsValidTarget(W.Range)))
            {
                if (enemy.HasBuff("rocketgrab2"))
                {
                    var t = ObjectManager.Get<AIHeroClient>().Where(i => i.IsAlly).ToList().Find(h => h.CharacterName.ToLower() == "blitzcrank" && h.Distance((AttackableUnit)player) < W.Range);
                    if (t != null)
                    {
                        if (W.Cast(t) == CastStates.SuccessfullyCasted)
                            return;
                    }
                }
                if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsStunned || enemy.IsRecalling())
                {
                    if (W.Cast(enemy) == CastStates.SuccessfullyCasted)
                        return;
                }
                if (W.GetPrediction(enemy).Hitchance == HitChance.Immobile)
                {
                    if (W.Cast(enemy) == CastStates.SuccessfullyCasted)
                        return;
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            if (player.IsDead)
                return;
            foreach (var spell in SpellList)
            {
                var menuBool = menu.Item("Draw" + spell.Slot + "Range").GetValue<MenuBool>();
                var menuColor = menu.Item("Draw" + spell.Slot + "Color").GetValue<MenuColor>();
                if (menuBool.Enabled)
                {
                    Render.Circle.DrawCircle(player.Position, spell.Range, menuColor.Color.ToSystemColor());
                }

            }
        }

        private static void ProcessLink(string key, object value)
        {
            if (value is MenuList)
            {
                stringLinks.Add(key, (MenuList)value);
            }
            else if (value is MenuSlider)
            {
                sliderLinks.Add(key, (MenuSlider)value);
            }

            else if (value is MenuKeyBind)
            {
                keyLinks.Add(key, (MenuKeyBind)value);
            }
            else
            {
                boolLinks.Add(key, (MenuBool)value);
            }
            
               
        }
        private float TotalDmg(AIBaseClient enemy, bool useQ, bool useE, bool useR, bool qRange)
        {
            var qaaDmg = new Double[] { 20, 40, 60, 80, 100 };
            var damage = 0d;
            var rTicks = sliderLinks["rTicks"].GetValue<MenuSlider>().Value;
            bool inQRange = ((qRange && enemy.InAutoAttackRange()) || qRange == false);
            //Base Q damage
            if (useQ && Q.IsReady() && inQRange)
            {
                damage += player.GetSpellDamage(enemy, SpellSlot.Q);
                damage += player.CalculateDamage(enemy, DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * player.TotalMagicalDamage + player.TotalAttackDamage);
            }

            // Q damage on AA
            if (useQ && !Q.IsReady() && player.HasBuff("viktorpowertransferreturn") && inQRange)
            {
                damage += player.CalculateDamage(enemy, DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * player.TotalMagicalDamage + player.TotalAttackDamage);
            }

            //E damage
            if (useE && E.IsReady())
            {
                if (player.HasBuff("viktoreaug") || player.HasBuff("viktorqeaug") || player.HasBuff("viktorqweaug"))
                    damage += player.GetSpellDamage(enemy, SpellSlot.E);
                else
                    damage += player.GetSpellDamage(enemy, SpellSlot.E);
            }

            //R damage + 2 ticks
            if (useR && R.Level > 0 && R.IsReady() && R.Instance.Name == "ViktorChaosStorm")
            {
                damage += player.GetSpellDamage(enemy, SpellSlot.R) * rTicks;
                damage += player.GetSpellDamage(enemy, SpellSlot.R);
            }

            // Ludens Echo damage
            if (Items.HasItem(player, 3285))
                damage += player.CalculateDamage(enemy, DamageType.Magical, 100 + player.FlatMagicDamageMod * 0.1);

            //sheen damage
            if (Items.HasItem(player, 3057))
                damage += player.CalculateDamage(enemy, DamageType.Physical, 0.5 * player.BaseAttackDamage);

            //lich bane dmg
            if (Items.HasItem(player, 3100))
                damage += player.CalculateDamage(enemy, DamageType.Magical, 0.5 * player.FlatMagicDamageMod + 0.75 * player.BaseAttackDamage);

            return (float)damage;
        }
        private float GetComboDamage(AIBaseClient enemy)
        {

            return TotalDmg(enemy, true, true, true, false);
        }
        private void SetupMenu()
        {

            menu = new Menu("Viktor", "Ryusung.Viktor credit Vasilyi", true);
            // Combo
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            var subMenu = menu.AddSubMenu(new Menu("Combo", "Combo"));

            
            ProcessLink("comboUseQ", subMenu.AddItem(new MenuBool("comboUseQ", "Use Q")));
            ProcessLink("comboUseW", subMenu.AddItem(new MenuBool("comboUseW", "Use W")));
            ProcessLink("comboUseE", subMenu.AddItem(new MenuBool("comboUseE", "Use E")));
            ProcessLink("comboUseR", subMenu.AddItem(new MenuBool("comboUseR", "Use R")));
            ProcessLink("qAuto", subMenu.AddItem(new MenuBool("qAuto", "Dont autoattack without passive")));
            ProcessLink("comboActive", subMenu.AddItem(new MenuKeyBind("comboActive", "Combo active", Keys.Space, KeyBindType.Press)));

            subMenu = menu.AddSubMenu(new Menu("Rconfig", "R config"));
            ProcessLink("HitR", subMenu.AddItem(new MenuList("HitR", "Auto R if: ", new string[] { "1 target", "2 targets", "3 targets", "4 targets", "5 targets" }, 3)));
            ProcessLink("AutoFollowR", subMenu.AddItem(new MenuBool("AutoFollowR", "Auto Follow R")));
            ProcessLink("rTicks", subMenu.AddItem(new MenuSlider("rTicks", "Ultimate ticks to count").SetValue(new Slider(2, 1, 14))));


            subMenu = subMenu.AddSubMenu(new Menu("Ronetarget", "R one target"));
            ProcessLink("forceR", subMenu.AddItem(new MenuKeyBind("forceR", "Force R on target", Keys.T, KeyBindType.Press)));
            ProcessLink("rLastHit", subMenu.AddItem(new MenuBool("rLastHit", "1 target ulti")));
            foreach (var hero in HeroManager.Enemies)
            {
                ProcessLink("RU" + hero.CharacterName, subMenu.AddItem(new MenuBool("RU" + hero.CharacterName, "Use R on: " + hero.CharacterName)));
            }


            subMenu = menu.AddSubMenu(new Menu("Testfeatures", "Test features"));
            ProcessLink("spPriority", subMenu.AddItem(new MenuBool("spPriority", "Prioritize kill over dmg")));


            // Harass
            subMenu = menu.AddSubMenu(new Menu("Harass", "Harass"));
            ProcessLink("harassUseQ", subMenu.AddItem(new MenuBool("harassUseQ", "Use Q")));
            ProcessLink("harassUseE", subMenu.AddItem(new MenuBool("harassUseE", "Use E")));
            ProcessLink("harassMana", subMenu.AddItem(new MenuSlider("harassMana", "Mana usage in percent (%)").SetValue(new Slider(30))));
            ProcessLink("eDistance", subMenu.AddItem(new MenuSlider("eDistance", "Harass range with E").SetValue(new Slider(maxRangeE, rangeE, maxRangeE))));
            ProcessLink("harassActive", subMenu.AddItem(new MenuKeyBind("harassActive", "Harass active", Keys.C, KeyBindType.Press)));

            // WaveClear
            subMenu = menu.AddSubMenu(new Menu("WaveClear", "WaveClear"));
            ProcessLink("waveUseQ", subMenu.AddItem(new MenuBool("waveUseQ", "Use Q")));
            ProcessLink("waveUseE", subMenu.AddItem(new MenuBool("waveUseE", "Use E")));
            ProcessLink("waveNumE", subMenu.AddItem(new MenuSlider("waveNumE", "Minions to hit with E").SetValue(new Slider(2, 1, 10))));
            ProcessLink("waveMana", subMenu.AddItem(new MenuSlider("waveMana", "Mana usage in percent (%)").SetValue(new Slider(30))));
            ProcessLink("waveActive", subMenu.AddItem(new MenuKeyBind("waveActive", "WaveClear active", Keys.V, KeyBindType.Press)));
            ProcessLink("jungleActive", subMenu.AddItem(new MenuKeyBind("jungleActive", "JungleClear active", Keys.G, KeyBindType.Press)));

            subMenu = menu.AddSubMenu(new Menu("LastHit", "LastHit"));
            ProcessLink("waveUseQLH", subMenu.AddItem(new MenuKeyBind("waveUseQLH", "Use Q", Keys.A, KeyBindType.Press)));

            // Harass
            subMenu = menu.AddSubMenu(new Menu("Flee", "Flee"));
            ProcessLink("FleeActive", subMenu.AddItem(new MenuKeyBind("FleeActive", "Flee mode", Keys.Z, KeyBindType.Press)));

            // Misc
            subMenu = menu.AddSubMenu(new Menu("Misc", "Misc"));
            ProcessLink("rInterrupt", subMenu.AddItem(new MenuBool("rInterrupt", "Use R to interrupt dangerous spells")));
            ProcessLink("wInterrupt", subMenu.AddItem(new MenuBool("wInterrupt", "Use W to interrupt dangerous spells")));
            ProcessLink("autoW", subMenu.AddItem(new MenuBool("autoW", "Use W to continue CC")));
            ProcessLink("miscGapcloser", subMenu.AddItem(new MenuBool("miscGapcloser", "Use W against gapclosers")));

            // Drawings
            subMenu = menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            ProcessLink("drawRangeQ", subMenu.AddSpellDraw(SpellSlot.Q));
            ProcessLink("drawRangeW", subMenu.AddSpellDraw(SpellSlot.W));
            ProcessLink("drawRangeE", subMenu.AddSpellDraw(SpellSlot.E));
            ProcessLink("drawRangeR", subMenu.AddSpellDraw(SpellSlot.R));
            menu.Attach();
        }
    }
}

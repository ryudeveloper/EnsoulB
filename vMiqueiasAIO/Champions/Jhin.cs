using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SPrediction.MinionManager;
using SPrediction;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;

namespace vMiqueiasAIO.Champions
{
    class Jhin
    {
        private Menu Config;
        private Spell E, Q, R, W;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        private bool Ractive = false;
        public AIHeroClient Player { get { return ObjectManager.Player; } }
        private Vector3 rPosLast;
        private AIHeroClient rTargetLast;
        private Vector3 rPosCast;

        private Items.Item
                    FarsightOrb = new Items.Item(3342, 4000f),
                    ScryingOrb = new Items.Item(3363, 3500f);

        private static string[] Spells =
        {
            "katarinar","drain","consume","absolutezero", "staticfield","reapthewhirlwind","jinxw","jinxr","shenstandunited","threshe","threshrpenta","threshq","meditate","caitlynpiltoverpeacemaker", "volibearqattack",
            "cassiopeiapetrifyinggaze","ezrealtrueshotbarrage","galioidolofdurand","luxmalicecannon", "missfortunebullettime","infiniteduress","alzaharnethergrasp","lucianq","velkozr","rocketgrabmissile"
        };
        private static bool LaneClear = false, None = false, Farm = false, Combo = false;

        private static int HitChanceNum = 4, tickNum = 4, tickIndex = 0;
        public Jhin()
        {

            Notifications.Add(new Notification("vMiqueias AIO fuck WWapper", "Jhin credit Sebby"));
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 2500);
            E = new Spell(SpellSlot.E, 760);
            R = new Spell(SpellSlot.R, 3500);

            W.SetSkillshot(0.75f, 40, 10000, false, false, SkillshotType.Line);
            E.SetSkillshot(1f, 120, 1600, false, false, SkillshotType.Circle);
            R.SetSkillshot(0.24f, 80, 5000, false, false, SkillshotType.Line);

            Config = new Menu("Jhin", "DH.Jhin", true);
            Menu QConfig = new Menu("QConfig", "QConfig");
            Menu WConfig = new Menu("WConfig", "WConfig");
            Menu EConfig = new Menu("EConfig", "EConfig");
            Menu RConfig = new Menu("RConfig", "RConfig");
            Menu Draw = new Menu("Draw", "Draw");
            Menu Farm = new Menu("Farm", "Farm");

           Draw.Add(new MenuBool("qRange", "Q range", false));
           Draw.Add(new MenuBool("wRange", "W range", false));
           Draw.Add(new MenuBool("eRange", "E range", false));
           Draw.Add(new MenuBool("rRange", "R range", false));
           Draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true));
           Draw.Add(new MenuBool("rRangeMini", "R range minimap", true));

           QConfig.Add(new MenuBool("autoQ", "Auto Q", true));
           QConfig.Add(new MenuBool("harrasQ", "Harass Q", true));
           QConfig.Add(new MenuBool("Qminion", "Q on minion", true));

           WConfig.Add(new MenuBool("autoW", "Auto W", true));
           WConfig.Add(new MenuBool("autoWcombo", "Auto W only in combo", false));
           WConfig.Add(new MenuBool("harrasW", "Harass W", true));
           WConfig.Add(new MenuBool("Wmark", "W marked only (main target)", true));
           WConfig.Add(new MenuBool("Wmarkall", "W marked (all enemys)", true));
           WConfig.Add(new MenuBool("Waoe", "W aoe (above 2 enemy)", true));
           WConfig.Add(new MenuBool("autoWcc", "Auto W CC enemy", true));
           WConfig.Add(new MenuSlider("MaxRangeW", "Max W range", 2500, 0, 2500));

           EConfig.Add(new MenuBool("autoE", "Auto E on hard CC", true));
           EConfig.Add(new MenuBool("bushE", "Auto E bush", true));
           EConfig.Add(new MenuBool("Espell", "E on special spell detection", true));
           EConfig.Add(new MenuList("EmodeCombo", "E combo mode", new[] { "always", "run - cheese", "disable" }, 1));
           EConfig.Add(new MenuSlider("Eaoe", "Auto E x enemies", 3, 0, 5));
           EConfig.Add(new MenuList("EmodeGC", "Gap Closer position mode", new[] { "Dash end position", "My hero position" }, 0));
            foreach (var enemy in GameObjects.EnemyHeroes)
               EConfig.Add(new MenuBool("EGCchampion" + enemy.CharacterName, enemy.CharacterName, true));

           RConfig.Add(new MenuBool("autoR", "Enable R", true));
           RConfig.Add(new MenuBool("Rvisable", "Don't shot if enemy is not visable", false));
           RConfig.Add(new MenuBool("Rks", "Auto R if can kill in 3 hits", true));
           RConfig.Add(new MenuKeyBind("useR", "Semi-manual cast R key", System.Windows.Forms.Keys.T, KeyBindType.Press)); //32 == space
           RConfig.Add(new MenuSlider("MaxRangeR", "Max R range", 3000, 0, 3500));
           RConfig.Add(new MenuSlider("MinRangeR", "Min R range", 1000, 0, 3500));
           RConfig.Add(new MenuSlider("Rsafe", "R safe area", 1000, 0, 2000));
           RConfig.Add(new MenuBool("trinkiet", "Auto blue trinkiet", true));

            //foreach (var enemy in GameObjects.EnemyHeroes)
            //   Harras.Add(new MenuBool("harras" + enemy.CharacterName, enemy.CharacterName));

           Farm.Add(new MenuBool("farmQ", "Lane clear Q", true));
           Farm.Add(new MenuBool("farmW", "Lane clear W", true));
           Farm.Add(new MenuBool("farmE", "Lane clear E", true));
           Farm.Add(new MenuSlider("Mana", "LaneClear Mana", 40, 0, 100));
           Farm.Add(new MenuSlider("LCminions", "LaneClear minimum minions", 3, 0, 10));
           Farm.Add(new MenuBool("jungleE", "Jungle clear E", true));
           Farm.Add(new MenuBool("jungleQ", "Jungle clear Q", true));
           Farm.Add(new MenuBool("jungleW", "Jungle clear W", true));

            Config.Add(new MenuBool("manaDisable", "Disable Mana Manager", false));
            Config.Add(new MenuBool("credit", "Credit: Sebby", false));

            Config.Add(QConfig);
            Config.Add(WConfig);
            Config.Add(EConfig);
            Config.Add(RConfig);
            Config.Add(Draw);
            Config.Add(Farm);
            Config.Attach();
            Drawing.OnDraw += Drawing_OnDraw;
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.R)
            {
                if (Config["RConfig"].GetValue<MenuBool>("trinkiet") && !IsCastingR)
                {
                    if (Player.Level < 9)
                        ScryingOrb.Range = 2500;
                    else
                        ScryingOrb.Range = 3500;

                    if (ScryingOrb.IsReady)
                        ScryingOrb.Cast(rPosLast);
                    if (FarsightOrb.IsReady)
                        FarsightOrb.Cast(rPosLast);
                }
            }
        }

        private void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name.ToLower() == "jhinr")
            {
                rPosCast = args.End;
            }
            if (!E.IsReady() || sender.IsMinion || !sender.IsEnemy || !Config["EConfig"].GetValue<MenuBool>("Espell") || !sender.IsValid() || !sender.IsValidTarget(E.Range))
                return;

            var foundSpell = Spells.Find(x => args.SData.Name.ToLower() == x);
            if (foundSpell != null)
            {
                E.Cast(sender.Position);
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (E.IsReady() && Player.Mana > RMANA + WMANA)
            {
                var t = sender;
                if (t.IsValidTarget(W.Range) && Config["EConfig"].GetValue<MenuBool>("EGCchampion" + t.CharacterName))
                {
                    if (Config["EConfig"].GetValue<MenuList>("EmodeGC").SelectedValue == "Dash end position")
                        E.Cast(args.EndPosition);
                    else
                        E.Cast(Player.Position);
                }
            }
        }
        private static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            if (LagFree(0))
            {
                SetMana();
                Jungle();
            }

            if (LagFree(1) && (R.IsReady() || IsCastingR) && Config["RConfig"].GetValue<MenuBool>("autoR"))
                LogicR();
            //Game.Print(")
            //Game.Print(R.Instance.Name);
            //Game.Print(R.Instance.SData.Name);
            //Game.Print(R.Name);
            if (Config["RConfig"].GetValue<MenuKeyBind>("useR").Active)
            {
                Orbwalker.MovementState = false;
                Orbwalker.AttackState = false;
                return;
            }
            else
            {
                Orbwalker.MovementState = true;
                Orbwalker.AttackState = true;
            }


            if (LagFree(4) && E.IsReady() && Orbwalker.CanMove())
                LogicE();

            if (LagFree(2) && Q.IsReady() && Config["QConfig"].GetValue<MenuBool>("autoQ"))
                LogicQ();

            if (LagFree(3) && W.IsReady() && !Player.Spellbook.IsAutoAttack && Config["WConfig"].GetValue<MenuBool>("autoW"))
                LogicW();

            Combo = Orbwalker.ActiveMode == OrbwalkerMode.Combo;
            Farm = (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear ) || Orbwalker.ActiveMode == OrbwalkerMode.Harass;
            None = Orbwalker.ActiveMode == OrbwalkerMode.None;
            LaneClear = Orbwalker.ActiveMode == OrbwalkerMode.LaneClear;
            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;

        }

        private void LogicR()
        {
            if (!IsCastingR)
                R.Range = Config["RConfig"].GetValue<MenuSlider>("MaxRangeR").Value;
            else
                R.Range = 3500;

            var t = TargetSelector.GetTarget(R.Range);
            if (t.IsValidTarget())
            {
                rPosLast = R.GetPrediction(t).CastPosition;
                if (Config["RConfig"].GetValue<MenuKeyBind>("useR").Active && !IsCastingR)
                {
                    R.Cast(rPosLast);
                    rTargetLast = t;
                }

                if (!IsCastingR && Config["RConfig"].GetValue<MenuBool>("Rks")
                    && GetRdmg(t) * 4 > t.Health && t.CountAllyHeroesInRange(700) == 0 && Player.CountEnemyHeroesInRange(Config["RConfig"].GetValue<MenuSlider>("Rsafe").Value) == 0
                    && Player.Distance(t) > Config["RConfig"].GetValue<MenuSlider>("MinRangeR").Value
                    && !Player.IsUnderEnemyTurret())
                {
                    R.Cast(rPosLast);
                    rTargetLast = t;
                }
                if (IsCastingR)
                {
                    var enemies = GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range)).OrderBy(enemy => enemy.Health);

                    //Game.Print("Casting:" + enemies.Count());
                    if (t.IsValidTarget(R.Range))
                    {
                        //Game.Print("Cast R to Selected");
                        R.Cast(t);
                    }
                        
                    else
                    {
                        foreach (var enemy in enemies)
                        {
                            //Game.Print("Cast R on Enemies");
                            R.Cast(t);
                            rPosLast = R.GetPrediction(enemy).CastPosition;
                            rTargetLast = enemy;
                        }
                    }
                }
            }
            else if (IsCastingR && rTargetLast != null && !rTargetLast.IsDead)
            {
                if (!Config["RConfig"].GetValue<MenuBool>("Rvisable") && rTargetLast.IsValidTarget(R.Range))
                {//InCone(rTargetLast.Position) && InCone(rPosLast))
                    //Game.Print("Cast R on Last target");
                    R.Cast(rPosLast);
                }
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range);
            if (t.IsValidTarget())
            {
                var wDmg = GetWdmg(t);
                if (wDmg > t.Health - Player.CalculateDamage(t, DamageType.Physical, 1))
                    CastSpell(W, t);

                if (Config["WConfig"].GetValue<MenuBool>("autoWcombo") && !Combo)
                    return;

                if (Player.CountEnemyHeroesInRange(400) > 1 || Player.CountEnemyHeroesInRange(250) > 0)
                    return;

                if (t.HasBuff("jhinespotteddebuff") || !Config["WConfig"].GetValue<MenuBool>("Wmark"))
                {
                    if (Player.Distance(t) < Config["WConfig"].GetValue<MenuSlider>("MaxRangeW").Value)
                    {
                        if (Combo && Player.Mana > RMANA + WMANA)
                            CastSpell(W, t);
                        else if (Farm && Config["WConfig"].GetValue<MenuBool>("harrasW")
                            && Player.Mana > RMANA + WMANA + QMANA + WMANA)
                            CastSpell(W, t);
                    }
                }

                if (!None && Player.Mana > RMANA + WMANA)
                {
                    if (Config["WConfig"].GetValue<MenuBool>("Waoe"))
                        W.CastIfWillHit(t, 2);

                    if (Config["WConfig"].GetValue<MenuBool>("autoWcc"))
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && !Orbwalker.CanMove()))
                            CastSpell(W, enemy);
                    }
                    if (Config["WConfig"].GetValue<MenuBool>("Wmarkall"))
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && enemy.HasBuff("jhinespotteddebuff")))
                            CastSpell(W, enemy);
                    }
                }
            }
            if (LaneClear && Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value && Config["Farm"].GetValue<MenuBool>("farmW") && Player.Mana > RMANA + WMANA)
            {
                var minionList = GameObjects.GetMinions(Player.Position, W.Range);
                var farmPosition = W.GetLineFarmLocation(minionList, W.Width);

                if (farmPosition.MinionsHit >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                    W.Cast(farmPosition.Position);
            }
        }
        private void CastSpell(Spell s, AIBaseClient target)
        {
            s.Cast(target);
        }

        private void LogicE()
        {
            if (Config["EConfig"].GetValue<MenuBool>("autoE"))
            {
                //var trapPos = Orbwalker.GetTrapPos(E.Range);
                //if (!trapPos.IsZero)
                //    E.Cast(trapPos);

                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range) && !Orbwalker.CanMove()))
                    E.Cast(enemy);
            }

            var t = TargetSelector.GetTarget(E.Range);
            if (t.IsValidTarget() && Config["EConfig"].GetValue<MenuList>("EmodeCombo").SelectedValue != "disable") //"always", "run - cheese", "disable"
            {
                if (Combo && !Player.Spellbook.IsAutoAttack)
                {
                    if (Config["EConfig"].GetValue<MenuList>("EmodeCombo").SelectedValue == "run - cheese")
                    {
                        if (E.GetPrediction(t).CastPosition.Distance(t.Position) > 100)
                        {
                            if (Player.Position.Distance(t.Position) > Player.Position.Distance(t.Position))
                            {
                                if (t.Position.Distance(Player.Position) < t.Position.Distance(Player.Position))
                                    CastSpell(E, t);
                            }
                            else
                            {
                                if (t.Position.Distance(Player.Position) > t.Position.Distance(Player.Position))
                                    CastSpell(E, t);
                            }
                        }
                    }
                    else
                    {
                        CastSpell(E, t);
                    }
                }

                E.CastIfWillHit(t, Config["EConfig"].GetValue<MenuSlider>("Eaoe").Value);
            }
            else if (LaneClear && Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value && Config["Farm"].GetValue<MenuBool>("farmE"))
            {
                var minionList = GameObjects.GetMinions(Player.Position, E.Range);
                var farmPosition = E.GetCircularFarmLocation(minionList, E.Width);

                if (farmPosition.MinionsHit >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                    E.Cast(farmPosition.Position);
            }
        }

        private void LogicQ()
        {
            var torb = Orbwalker.GetTarget();

            if (torb == null || torb.Type != GameObjectType.AIHeroClient)
            {
                if (Config["QConfig"].GetValue<MenuBool>("Qminion"))
                {
                    var t = TargetSelector.GetTarget(Q.Range + 300);
                    if (t.IsValidTarget())
                    {
                        var pos = Prediction.GetFastUnitPosition(t, 0.1f);
                        //GameObjects.EnemyMinions.Where(m => m.Distance(pos) <= 300 && m.IsValidTarget(Q.Range)).OrderBy(x => x.Distance(t)).FirstOrDefault();
                        var minion = GameObjects.EnemyMinions.Where(m => m.Distance(pos) <= 300 && m.IsValidTarget(Q.Range)).OrderBy(x => x.Distance(t)).FirstOrDefault();
                        if (minion.IsValidTarget())
                        {
                            if (t.Health < GetQdmg(t))
                                Q.CastOnUnit(minion);
                            if (Combo && Player.Mana > RMANA + EMANA)
                                Q.CastOnUnit(minion);
                            else if (Farm && Config["QConfig"].GetValue<MenuBool>("harrasQ") && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                                Q.CastOnUnit(minion);
                        }
                    }
                }

            }
            else if (!Orbwalker.CanAttack() && !Player.Spellbook.IsAutoAttack)
            {
                var t = torb as AIHeroClient;
                if (t.Health < GetQdmg(t) + GetWdmg(t))
                    Q.CastOnUnit(t);
                if (Combo && Player.Mana > RMANA + QMANA)
                    Q.CastOnUnit(t);
                else if (Farm && Config["QConfig"].GetValue<MenuBool>("harrasQ") && Player.Mana > RMANA + QMANA + WMANA + EMANA)
                    Q.CastOnUnit(t);
            }
            if (LaneClear && Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value && Config["Farm"].GetValue<MenuBool>("farmQ"))
            {
                var minionList = GameObjects.GetMinions(Player.Position, Q.Range);

                if (minionList.Count >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                {
                    var minionAttack = minionList.FirstOrDefault(x => Q.GetDamage(x) > HealthPrediction.GetPrediction(x, 300));
                    if (minionAttack.IsValidTarget())
                        Q.CastOnUnit(minionAttack);
                }

            }
        }


        private bool InCone(Vector3 Position)
        {
            var range = R.Range;
            var angle = 70f * (float)Math.PI / 180;
            var end2 = rPosCast.ToVector2() - Player.Position.ToVector2();
            var edge1 = end2.Rotated(-angle / 2);
            var edge2 = edge1.Rotated(angle);

            var point = Position.ToVector2() - Player.Position.ToVector2();
            if (point.Distance(new Vector2()) < range * range && edge1.CrossProduct(point) > 0 && point.CrossProduct(edge2) > 0)
                return true;

            return false;

        }

        private void Jungle()
        {
            if (LaneClear)
            {
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth).ToList<AIBaseClient>();
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];

                    if (W.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleW"))
                    {
                        W.Cast(mob.Position);
                        return;
                    }
                    if (E.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleE"))
                    {
                        E.Cast(mob.Position);
                        return;
                    }
                    if (Q.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleQ"))
                    {
                        Q.CastOnUnit(mob);
                        return;
                    }
                }
            }
        }

        private bool IsCastingR { get { return R.Name == "JhinRShot"; } }

        private double GetRdmg(AIBaseClient target)
        {
            var damage = (-25 + 75 * R.Level + 0.2 * Player.FlatPhysicalDamageMod) * (1 + (100 - target.HealthPercent) * 0.02);

            return Player.CalculateDamage(target, DamageType.Physical, damage);
        }

        private double GetWdmg(AIBaseClient target)
        {
            var damage = 55 + W.Level * 35 + 0.7 * Player.FlatPhysicalDamageMod;

            return Player.CalculateDamage(target, DamageType.Physical, damage);
        }

        private double GetQdmg(AIBaseClient target)
        {
            var damage = 35 + Q.Level * 25 + 0.4 * Player.FlatPhysicalDamageMod;

            return Player.CalculateDamage(target, DamageType.Physical, damage);
        }
        private void SetMana()
        {
            if ((Config.GetValue<MenuBool>("manaDisable") && Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Mana;
            WMANA = W.Mana;
            EMANA = E.Mana;

            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Mana;
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("rRangeMini"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1);
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("qRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("wRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("eRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("rRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }
    }
}

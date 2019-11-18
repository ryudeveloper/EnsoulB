using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using Utility = EnsoulSharp.SDK.Utility;
using SPrediction;

namespace RyusungAIO.Champions
{
    class Draven
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the e.
        /// </summary>
        /// <value>
        ///     The e.
        /// </value>
        public Spell E { get; set; }

        /// <summary>
        ///     Gets the mana percent.
        /// </summary>
        /// <value>
        ///     The mana percent.
        /// </value>
        public float ManaPercent
        {
            get
            {
                return Player.Mana / Player.MaxMana * 100;
            }
        }

        /// <summary>
        ///     Gets or sets the menu.
        /// </summary>
        /// <value>
        ///     The menu.
        /// </value>
        public Menu Menu { get; set; }

        /// <summary>
        ///     Gets or sets the orbwalker.
        /// </summary>
        /// <value>
        ///     The orbwalker.
        /// </value>

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        public AIHeroClient Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        /// <summary>
        ///     Gets or sets the q.
        /// </summary>
        /// <value>
        ///     The q.
        /// </value>
        public Spell Q { get; set; }

        /// <summary>
        ///     Gets the q count.
        /// </summary>
        /// <value>
        ///     The q count.
        /// </value>
        public int QCount
        {
            get
            {
                return (Player.HasBuff("dravenspinning") ? 1 : 0)
                       + (Player.HasBuff("dravenspinningleft") ? 1 : 0) + QReticles.Count;
            }
        }

        /// <summary>
        ///     Gets or sets the q reticles.
        /// </summary>
        /// <value>
        ///     The q reticles.
        /// </value>
        public List<QRecticle> QReticles { get; set; }

        /// <summary>
        ///     Gets or sets the r.
        /// </summary>
        /// <value>
        ///     The r.
        /// </value>
        public Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the w.
        /// </summary>
        /// <value>
        ///     The w.
        /// </value>
        public Spell W { get; set; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the last axe move time.
        /// </summary>
        private int LastAxeMoveTime { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        public Draven()
        {
            // Create spells
            Q = new Spell(SpellSlot.Q, Player.GetRealAutoAttackRange());
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1050);
            R = new Spell(SpellSlot.R);

            E.SetSkillshot(0.25f, 130, 1400, false, false, SkillshotType.Line);
            R.SetSkillshot(0.4f, 160, 2000, true, false, SkillshotType.Line);

            QReticles = new List<QRecticle>();

            CreateMenu();

            //AIBaseClient.OnNewPath += AIBaseClient_OnNewPath;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Gapcloser.OnGapcloser += AntiGapcloserOnOnEnemyGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2OnOnInterruptableTarget;
            Drawing.OnDraw += DrawingOnOnDraw;
            EnsoulSharp.SDK.Events.Tick.OnTick += GameOnOnUpdate;
            //Orbwalker.OnAction += OnActionDelegate;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Called on an enemy gapcloser.
        /// </summary>
        /// <param name="gapcloser">The gapcloser.</param>
        /// 


//        private void OnActionDelegate(
//    Object sender,
//    OrbwalkerActionArgs args
//)
//        {

//            if(args.Type == OrbwalkerType.AfterAttack)
//            {

//                CatchAxe();
//            }
//        }
        private void AntiGapcloserOnOnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (!Menu["Misc"].GetValue<MenuBool>("UseEGapcloser") || !E.IsReady()
                || !sender.IsValidTarget(E.Range) || Player.Distance(args.EndPosition) > 200)
            {
                return;
            }

            E.Cast(args.EndPosition);
        }

        /// <summary>
        ///     Catches the axe.
        /// </summary>
        private void CatchAxe()
        {
            var catchOption = Menu["axeSetting"].GetValue<MenuList>("AxeMode").SelectedValue; //"Combo", "Any", "Always"

            if (((catchOption == "Combo" && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                 || (catchOption == "Any" && Orbwalker.ActiveMode != OrbwalkerMode.None))
                || catchOption == "Always")
            {

                //Game.Print(QReticles.Count());
                var bestReticle =
                    QReticles.Where(
                        x =>
                        x.Object.Position.Distance(Game.CursorPos)
                        < Menu["axeSetting"].GetValue<MenuSlider>("CatchAxeRange").Value)
                        .OrderBy(x => x.Position.Distance(Player.Position))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .ThenBy(x => x.ExpireTime)
                        .FirstOrDefault();

                if (bestReticle != null && bestReticle.Object.Position.Distance(Player.Position) > 100)
                {
                    var eta = 1000 * (Player.Distance(bestReticle.Position) / Player.MoveSpeed);
                    var expireTime = bestReticle.ExpireTime - Environment.TickCount;

                    if (eta >= expireTime && Menu["axeSetting"].GetValue<MenuBool>("UseWForQ"))
                    {
                        W.Cast();
                    }

                    if (Menu["axeSetting"].GetValue<MenuBool>("DontCatchUnderTurret")) // debug this?
                    {
                        // If we're under the turret as well as the axe, catch the axe
                        if (!bestReticle.Position.IsUnderEnemyTurret())
                        {

                            Orbwalker.SetOrbwalkerPosition(bestReticle.Position);

                        }
                        //else if (!bestReticle.Position.IsUnderEnemyTurret())
                        //{
                        //    Game.Print("Catch2");
                        //    Orbwalker.SetOrbwalkerPosition(bestReticle.Position);
                        //}
                    }
                    else
                    {
                        Orbwalker.SetOrbwalkerPosition(bestReticle.Position);
                    }
                }
                else
                {
                    Orbwalker.SetOrbwalkerPosition(Game.CursorPos);
                }
            }
            else
            {
                Orbwalker.SetOrbwalkerPosition(Game.CursorPos);
            }
        }


        /// <summary>
        ///     Does the combo.
        /// </summary>
        private void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = Menu["Combo"].GetValue<MenuBool>("UseQCombo");
            var useW = Menu["Combo"].GetValue<MenuBool>("UseWCombo");
            var useE = Menu["Combo"].GetValue<MenuBool>("UseECombo");
            var useR = Menu["Combo"].GetValue<MenuBool>("UseRCombo");

            if (useQ && QCount < Menu["axeSetting"].GetValue<MenuSlider>("MaxAxes").Value - 1 && Q.IsReady()
                && target.InAutoAttackRange() && !Player.Spellbook.IsAutoAttack)
            {
                Q.Cast();
            }

            if (useW && W.IsReady()
                && ManaPercent > Menu["Misc"].GetValue<MenuSlider>("UseWManaPercent").Value)
            {
                if (Menu["Misc"].GetValue<MenuBool>("UseWSetting"))
                {
                    W.Cast();
                }
                else
                {
                    if (!Player.HasBuff("dravenfurybuff"))
                    {
                        W.Cast();
                    }
                }
            }

            if (useE && E.IsReady())
            {
                E.Cast(target);
            }

            if (!useR || !R.IsReady())
            {
                return;
            }

            // Patented Advanced Algorithms D321987
            var killableTarget =
                GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(2000))
                    .FirstOrDefault(
                        x =>
                        Player.GetSpellDamage(x, SpellSlot.R) * 2 > x.Health
                        && (!x.InAutoAttackRange() || Player.CountEnemyHeroesInRange(E.Range) > 2));

            if (killableTarget != null)
            {
                R.Cast(killableTarget);
            }
        }

        /// <summary>
        ///     Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            Menu = new Menu("Draven", "RY.Draven", true);


            // Combo
            var comboMenu = new Menu("Combo", "Xombo");
            comboMenu.Add(new MenuBool("UseQCombo", "Use Q"));
            comboMenu.Add(new MenuBool("UseWCombo", "Use W"));
            comboMenu.Add(new MenuBool("UseECombo", "Use E"));
            comboMenu.Add(new MenuBool("UseRCombo", "Use R"));
            Menu.Add(comboMenu);

            // Harass
            var harassMenu = new Menu("Harass", "harass");
            harassMenu.Add(new MenuBool("UseEHarass", "Use E"));
            harassMenu.Add(
                new MenuKeyBind("UseHarassToggle", "Harass! (Toggle)", System.Windows.Forms.Keys.H, KeyBindType.Toggle));
            Menu.Add(harassMenu);

            // Lane Clear
            var laneClearMenu = new Menu("waveclear", "Wave Clear");
            laneClearMenu.Add(new MenuBool("UseQWaveClear", "Use Q"));
            laneClearMenu.Add(new MenuBool("UseWWaveClear", "Use W"));
            laneClearMenu.Add(new MenuBool("UseEWaveClear", "Use E", false));
            laneClearMenu.Add(new MenuSlider("WaveClearManaPercent", "Mana Percent", 50));
            Menu.Add(laneClearMenu);

            // Axe Menu
            var axeMenu = new Menu("axeSetting", "Axe Settings");
            axeMenu.Add(
                new MenuList("AxeMode", "Catch Axe on Mode:", new[] { "Combo", "Any", "Always" }, 2));
            axeMenu.Add(new MenuSlider("CatchAxeRange", "Catch Axe Range", 800, 120, 1500));
            axeMenu.Add(new MenuSlider("MaxAxes", "Maximum Axes", 2, 1, 3));
            axeMenu.Add(new MenuBool("UseWForQ", "Use W if Axe too far"));
            axeMenu.Add(new MenuBool("DontCatchUnderTurret", "Don't Catch Axe Under Turret"));
            Menu.Add(axeMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Drawing");
            drawMenu.Add(new MenuBool("DrawE", "Draw E"));
            drawMenu.Add(new MenuBool("DrawAxeLocation", "Draw Axe Location"));
            drawMenu.Add(new MenuBool("DrawAxeRange", "Draw Axe Catch Range"));
            Menu.Add(drawMenu);

            // Misc Menu
            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.Add(new MenuBool("UseWSetting", "Use W Instantly(When Available)", false));
            miscMenu.Add(new MenuBool("UseEGapcloser", "Use E on Gapcloser"));
            miscMenu.Add(new MenuBool("UseEInterrupt", "Use E to Interrupt"));
            miscMenu.Add(new MenuSlider("UseWManaPercent", "Use W Mana Percent", 50));
            miscMenu.Add(new MenuBool("UseWSlow", "Use W if Slowed"));
            Menu.Add(miscMenu);

            Menu.Attach();
        }

        /// <summary>
        ///     Called when the game draws itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void DrawingOnOnDraw(EventArgs args)
        {
            var drawE = Menu["Drawing"].GetValue<MenuBool>("DrawE");
            var drawAxeLocation = Menu["Drawing"].GetValue<MenuBool>("DrawAxeLocation");
            var drawAxeRange = Menu["Drawing"].GetValue<MenuBool>("DrawAxeRange");

            if (drawE)
            {
                Render.Circle.DrawCircle(
                    ObjectManager.Player.Position,
                    E.Range,
                    E.IsReady() ? System.Drawing.Color.Aqua : System.Drawing.Color.Red);
            }

            if (drawAxeLocation)
            {
                var bestAxe =
                    QReticles.Where(
                        x =>
                        x.Position.Distance(Game.CursorPos) < Menu["axeSetting"].GetValue<MenuSlider>("CatchAxeRange").Value)
                        .OrderBy(x => x.Position.Distance(Player.Position))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .FirstOrDefault();

                if (bestAxe != null)
                {
                    Render.Circle.DrawCircle(bestAxe.Position, 120, System.Drawing.Color.LimeGreen);
                }

                foreach (var axe in
                    QReticles.Where(x => x.Object.NetworkId != (bestAxe == null ? 0 : bestAxe.Object.NetworkId)))
                {
                    Render.Circle.DrawCircle(axe.Position, 120, System.Drawing.Color.Yellow);
                }
            }

            if (drawAxeRange)
            {
                Render.Circle.DrawCircle(
                    Game.CursorPos,
                    Menu["axeSetting"].GetValue<MenuSlider>("CatchAxeRange").Value,
                     System.Drawing.Color.DodgerBlue);
            }
        }

        /// <summary>
        ///     Called when a game object is created.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            //Game.Print(sender.Name);
            if (!sender.Name.Contains("Q_reticle_self"))
            {
                return;
            }

            QReticles.Add(new QRecticle(sender, Environment.TickCount + 1300));
            Utility.DelayAction.Add(1300, () => QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId));
        }

        /// <summary>
        ///     Called when a game object is deleted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
        }

        /// <summary>
        ///     Called when the game updates.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void GameOnOnUpdate(EventArgs args)
        {
            QReticles.RemoveAll(x => x.Object.IsDead);

            CatchAxe();
            if (W.IsReady() && Menu["Misc"].GetValue<MenuBool>("UseWSlow") && Player.HasBuffOfType(BuffType.Slow))
            {
                W.Cast();
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
            }

            if (Menu["Harass"].GetValue<MenuKeyBind>("UseHarassToggle").Active)
            {
                Harass();
            }
        }

        /// <summary>
        ///     Harasses the enemy.
        /// </summary>
        private void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (Menu["Harass"].GetValue<MenuBool>("UseEHarass") && E.IsReady())
            {
                E.Cast(target);
            }
        }

        /// <summary>
        ///     Interrupts an interruptable target.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="Interrupter2.InterruptableTargetEventArgs" /> instance containing the event data.</param>
        private void Interrupter2OnOnInterruptableTarget(
    AIHeroClient sender,
    Interrupter.InterruptSpellArgs args
)
        {
            if (!Menu["Misc"].GetValue<MenuBool>("UseEInterrupt") || !E.IsReady() || !sender.IsValidTarget(E.Range))
            {
                return;
            }

            if (args.DangerLevel == Interrupter.DangerLevel.Medium || args.DangerLevel == Interrupter.DangerLevel.High)
            {
                E.Cast(sender);
            }
        }

        /// <summary>
        ///     Clears the lane of minions.
        /// </summary>
        private void LaneClear()
        {
            var useQ = Menu["waveclear"].GetValue<MenuBool>("UseQWaveClear");
            var useW = Menu["waveclear"].GetValue<MenuBool>("UseWWaveClear");
            var useE = Menu["waveclear"].GetValue<MenuBool>("UseEWaveClear");

            if (ManaPercent < Menu["waveclear"].GetValue<MenuSlider>("WaveClearManaPercent").Value)
            {
                return;
            }

            if (useQ && QCount < Menu["axeSetting"].GetValue<MenuSlider>("MaxAxes").Value - 1 && Q.IsReady()
                && Orbwalker.GetTarget() is AIMinionClient && !Player.Spellbook.IsAutoAttack
                && !Player.IsWindingUp)
            {
                Q.Cast();
            }

            if (useW && W.IsReady()
                && ManaPercent > Menu["Misc"].GetValue<MenuSlider>("UseWManaPercent").Value)
            {
                if (Menu["Misc"].GetValue<MenuBool>("UseWSetting"))
                {
                    W.Cast();
                }
                else
                {
                    if (!Player.HasBuff("dravenfurybuff"))
                    {
                        W.Cast();
                    }
                }
            }

            if (!useE || !E.IsReady())
            {
                return;
            }

            var bestLocation = E.GetLineFarmLocation(GameObjects.GetMinions(E.Range));

            if (bestLocation.MinionsHit > 1)
            {
                E.Cast(bestLocation.Position);
            }
        }

        /// <summary>
        ///     Fired when the OnNewPath event is called.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectNewPathEventArgs" /> instance containing the event data.</param>
        private void AIBaseClient_OnNewPath(AIBaseClient sender, AIBaseClientNewPathEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            CatchAxe();
        }

        #endregion

        /// <summary>
        ///     A represenation of a Q circle on Draven.
        /// </summary>
        internal class QRecticle
        {
            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="QRecticle" /> class.
            /// </summary>
            /// <param name="rectice">The rectice.</param>
            /// <param name="expireTime">The expire time.</param>
            public QRecticle(GameObject rectice, int expireTime)
            {
                Object = rectice;
                ExpireTime = expireTime;
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the expire time.
            /// </summary>
            /// <value>
            ///     The expire time.
            /// </value>
            public int ExpireTime { get; set; }

            /// <summary>
            ///     Gets or sets the object.
            /// </summary>
            /// <value>
            ///     The object.
            /// </value>
            public GameObject Object { get; set; }

            /// <summary>
            ///     Gets the position.
            /// </summary>
            /// <value>
            ///     The position.
            /// </value>
            public Vector3 Position
            {
                get
                {
                    return Object.Position;
                }
            }

            #endregion
        }
    }
}

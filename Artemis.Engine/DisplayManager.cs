﻿#region Using Statements

using Artemis.Engine.Persistence;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;
using System.Windows.Forms;

#endregion

namespace Artemis.Engine
{
    public class DisplayManager
    {

        // The amounts by which the display area is offset by the window border.
        public const int WINDOW_BORDER_OFFSET_X = 8;
        public const int WINDOW_BORDER_OFFSET_Y = 30;

        private GameKernel game;
        private GraphicsDevice graphicsDevice;
        private GraphicsDeviceManager graphicsManager;
        private SpriteBatch spriteBatch;
        private GameWindow window;

        /// <summary>
        /// The current resolution of the window.
        /// </summary>
        public Resolution WindowResolution
        {
            get
            {
                return UserOptions.Get<Resolution>(UserOptions.Builtins.Resolution);
            }
            set
            {
                UserOptions.Set(UserOptions.Builtins.Resolution, value);
            }
        }

        /// <summary>
        /// Whether or not the current resolution is equal to the base resolution.
        /// </summary>
        public bool IsBaseResolution { get { return WindowResolution == GameConstants.BaseResolution; } }

        /// <summary>
        /// The ratio of the current resolution to the base resolution.
        /// </summary>
        public Vector2 ResolutionScale { get { return WindowResolution / GameConstants.BaseResolution; } }

        /// <summary>
        /// Whether or not the resolution has changed.
        /// </summary>
        public bool ResolutionChanged { get { return resChangedCounter > 0; } }
        private int resChangedCounter = 0;

        /// <summary>
        /// Whether or not the display is fullscreen.
        /// </summary>
        public bool Fullscreen 
        { 
            get 
            { 
                return UserOptions.Get<bool>(UserOptions.Builtins.Fullscreen); 
            } 
            private set 
            { 
                UserOptions.Set(UserOptions.Builtins.Fullscreen, value); 
            } 
        }

        /// <summary>
        /// Whether or not the mouse cursor is visible.
        /// </summary>
        public bool MouseVisible
        {
            get
            {
                return UserOptions.Get<bool>(UserOptions.Builtins.MouseVisible);
            }
            private set
            {
                UserOptions.Set(UserOptions.Builtins.MouseVisible, value);
            }
        }

        /// <summary>
        /// Whether or not the window is bordered.
        /// </summary>
        public bool Borderless
        {
            get
            {
                return UserOptions.Get<bool>(UserOptions.Builtins.Borderless);
            }
            private set
            {
                UserOptions.Set(UserOptions.Builtins.Borderless, value);
            }
        }

        /// <summary>
        /// Whether or not the display uses vertical synchronization.
        /// </summary>
        public bool VSync
        {
            get
            {
                return UserOptions.Get<bool>(UserOptions.Builtins.VSync);
            }
            set
            {
                UserOptions.Set(UserOptions.Builtins.VSync, value);
            }
        }

        /// <summary>
        /// The background colour of the display.
        /// </summary>
        public Color BackgroundColour { get; private set; }

        /// <summary>
        /// The title of the game window.
        /// </summary>
        public string WindowTitle
        {
            get
            {
                return window.Title;
            }
            private set
            {
                window.Title = value;
            }
        }

        /// <summary>
        /// Whether or not ReinitDisplayProperties has to be called. 
        /// </summary>
        private bool dirty;

        private HashSet<ResolutionRelativeObject> registeredResolutionChangeListeners
            = new HashSet<ResolutionRelativeObject>();
        private bool iteratingResolutionChangeListeners = false;
        private List<ResolutionRelativeObject> _toAdd = new List<ResolutionRelativeObject>();
        private List<ResolutionRelativeObject> _toRemove = new List<ResolutionRelativeObject>();

        internal DisplayManager( GameKernel game
                               , RenderPipeline renderPipeline
                               , Resolution baseResolution
                               , string windowTitle
                               , Color bgColor )
        {
            this.game            = game;
            this.graphicsDevice  = renderPipeline.GraphicsDevice;
            this.graphicsManager = renderPipeline.GraphicsDeviceManager;
            this.spriteBatch     = renderPipeline.SpriteBatch;

            window = game.Window;
            WindowResolution = baseResolution;

            WindowTitle = windowTitle;
            BackgroundColour = bgColor;

            ReinitDisplayProperties(true);
        }

        internal void RegisterResolutionChangeListener(ResolutionRelativeObject obj)
        {
            if (iteratingResolutionChangeListeners)
                _toAdd.Add(obj);
            else
                registeredResolutionChangeListeners.Add(obj);
        }

        internal void RemoveResolutionChangeListener(ResolutionRelativeObject obj)
        {
            if (iteratingResolutionChangeListeners)
                _toRemove.Add(obj);
            else
                registeredResolutionChangeListeners.Remove(obj);
        }

        /// <summary>
        /// Initialize (or reinitialize) the properties of the screen form.
        /// </summary>
        internal void ReinitDisplayProperties(bool overrideDirty = false)
        {
            graphicsManager.SynchronizeWithVerticalRetrace = VSync;

            if (!dirty && !overrideDirty)
            {
                return;
            }

            game.IsMouseVisible = MouseVisible;
            window.IsBorderless = Borderless;

            graphicsManager.IsFullScreen              = Fullscreen;
            graphicsManager.PreferredBackBufferWidth  = WindowResolution.Width;
            graphicsManager.PreferredBackBufferHeight = WindowResolution.Height;

            // Center the display based on the native resolution.
            var form = (System.Windows.Forms.Form)Control.FromHandle(window.Handle);
            var position = new System.Drawing.Point(
                (Resolution.Native.Width - WindowResolution.Width) / 2,
                (Resolution.Native.Height - WindowResolution.Height) / 2
                );

            // This offset seems to width and height of the windows border,
            // so it accounts for the slight off-centering (unless the window
            // is larger than the native display).
            if (!Borderless)
            {
                position.X -= WINDOW_BORDER_OFFSET_X;
                position.Y -= WINDOW_BORDER_OFFSET_Y;
            }

            graphicsManager.ApplyChanges();

            /* We have to reposition the form after we apply changes to the graphics manager
             * otherwise if the user changes the resolution while in fullscreen, then goes into
             * windowed mode, the window would be position relative to the previous resolution
             * rather than the new resolution.
             */
            form.Location = position;

            dirty = false;
        }

        /// <summary>
        /// Toggle whether or not the display is fullscreen.
        /// </summary>
        public void ToggleFullscreen()
        {
            SetFullscreen(!Fullscreen);
        }

        /// <summary>
        /// Toggle whether or not the mouse is visible.
        /// </summary>
        public void ToggleMouseVisibility()
        {
            SetBorderless(!MouseVisible);
        }

        /// <summary>
        /// Toggle whether or not the window form has a border.
        /// </summary>
        public void ToggleBorderless()
        {
            SetBorderless(!Borderless);
        }

        /// <summary>
        /// Toggle whether or not vertical synchronization is used.
        /// </summary>
        public void ToggleVSync()
        {
            SetVSync(!VSync);
        }

        /// <summary>
        /// Set whether or not the display is fullscreen.
        /// </summary>
        /// <param name="state"></param>
        public void SetFullscreen(bool state)
        {
            if (state != Fullscreen && !GameConstants.FullscreenTogglable)
                throw UntogglableException("Fullscreen", GameConstants.XmlElements.FULLSCREEN_TOGGLABLE);
            Fullscreen = state;
            dirty = true;
        }

        /// <summary>
        /// Set whether or not the mouse is visible.
        /// </summary>
        /// <param name="state"></param>
        public void SetMouseVisibility(bool state)
        {
            if (state != MouseVisible && !GameConstants.MouseVisibilityTogglable)
                throw UntogglableException("Mouse visibility", GameConstants.XmlElements.MOUSE_VISIBILITY_TOGGLABLE);
            MouseVisible = state;
            dirty = true;
        }

        /// <summary>
        /// Set whether or not the window form has a border.
        /// </summary>
        /// <param name="state"></param>
        public void SetBorderless(bool state)
        {
            if (state != Borderless && !GameConstants.BorderTogglable)
                throw UntogglableException("Borderless", GameConstants.XmlElements.BORDER_TOGGLABLE);
            Borderless = state;
            dirty = true;
        }

        public void SetResolution(Resolution resolution)
        {
            if (resolution != WindowResolution && GameConstants.StaticResolution)
            {
                throw new DisplayManagerException(
                    String.Format(
                        "Cannot change resolution. (Game Property '{0}' set to true)",
                        GameConstants.XmlElements.STATIC_RESOLUTION
                        )
                    );   
            }
            if (GameConstants.OnlyLandscapeResolutions && !resolution.IsLandscape)
            {
                throw new DisplayManagerException(
                    String.Format(
                        "Cannot change resolution to '{0}'; resolutions must be landscape. " + 
                        "(GameProperty '{1}' set to true)", resolution, GameConstants.XmlElements.ONLY_LANDSCAPE_RESOLUTIONS
                        )
                    );
            }
            else if (GameConstants.OnlyPortraitResolutions && !resolution.IsPortrait)
            {
                throw new DisplayManagerException(
                    String.Format(
                        "Cannot change resolution to '{0}'; resolutions must be portrait. " +
                        "(GameProperty '{1}' set to true)", resolution, GameConstants.XmlElements.ONLY_PORTRAIT_RESOLUTIONS
                        )
                    );
            }

            var prev = WindowResolution;
            WindowResolution = resolution;
            dirty = true;

            iteratingResolutionChangeListeners = true;
            foreach (var obj in registeredResolutionChangeListeners)
            {
                if (obj.OnResolutionChanged != null)
                    obj.OnResolutionChanged(prev, resolution, ResolutionScale);
            }
            iteratingResolutionChangeListeners = false;

            if (_toAdd.Count > 0)
            {
                foreach (var obj in _toAdd)
                    registeredResolutionChangeListeners.Add(obj);
                _toAdd.Clear();
            }
            if (_toRemove.Count > 0)
            {
                foreach (var obj in _toRemove)
                    registeredResolutionChangeListeners.Remove(obj);
                _toRemove.Clear();
            }
        }

        /// <summary>
        /// Set whether or not vertical synchronization is used.
        /// </summary>
        /// <param name="state"></param>
        public void SetVSync(bool state)
        {
            VSync = state;
            dirty = true;
        }

        private static DisplayManagerException UntogglableException(string property, string element)
        {
            return new DisplayManagerException(
                String.Format(
                    "{0} is not set to be togglable. To change this, add a " +
                    "{1} element to the root element of the .gamesetup file.",
                    property, element
                    )
                );
        }

        /// <summary>
        /// Set the name of the window.
        /// </summary>
        /// <param name="name"></param>
        public void SetWindowTitle(string name)
        {
            WindowTitle = name;
        }

        public void SetBackgroundColour(Color colour)
        {
            BackgroundColour = colour;
        }
    }
}

using Rage;
using System;
using System.Linq;
using System.Windows.Forms;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// Provides methods to access the state of the Keyboard, taking into account
    /// the in game keyboard window
    /// </summary>
    internal static class Keyboard
    {
        /// <summary>
        /// An array of valid computer key modifiers
        /// </summary>
        internal static Keys[] Modifiers = { Keys.LControlKey, Keys.RControlKey, Keys.Alt, Keys.LShiftKey, Keys.RShiftKey };

        /// <summary>
        /// Indicates whether the on screen keyboard is currently open
        /// </summary>
        public static bool IsKeyboardOpen
        {
            get
            {
                int status = Natives.UpdateOnscreenKeyboard<int>();
                return status == 0;
            }
        }

        /// <summary>
        /// Returns whether the computer key is pressed. If the on screen keyboard
        /// is open, this method returns false.
        /// </summary>
        /// <param name="keyPressed">The <see cref="Keys"/> we are checking is pressed.</param>
        /// <param name="rightNow">If true, checks if the key is still pressed down during this frame render</param>
        /// <param name="ignoreModifiers">
        /// If true, this method will ignore whether any modifier keys are down. If false and a modifier key
        /// is also down, this method will always return false.
        /// </param>
        /// <returns></returns>
        /// <seealso cref="http://www.dev-c.com/nativedb/func/info/0cf2b696bbf945ae"/>
        /// <remarks>
        /// Native Status Codes:
        ///     0 - User still editing
        ///     1 - User has finished editing
        ///     2 - User has canceled editing
        ///     3 - Keyboard isn't active
        /// </remarks>
        internal static bool IsComputerKeyDown(Keys keyPressed, bool rightNow = false, bool ignoreModifiers = true)
        {
            if (!IsKeyboardOpen)
            {
                // If we are not ignoring modifiers, and a modifier key is down,
                // Then we return false
                if (!ignoreModifiers && IsAnyModifierKeyDownRightNow())
                {
                    return false;
                }

                return (rightNow) ? Rage.Game.IsKeyDownRightNow(keyPressed) : Rage.Game.IsKeyDown(keyPressed);
            }

            return false;
        }

        /// <summary>
        /// Returns whether any of the specified computer keys are pressed. If the on screen keyboard
        /// is open, this method returns false.
        /// </summary>
        /// <param name="KeyPressed">The <see cref="Keys"/> we are checking is pressed.</param>
        /// <returns></returns>
        /// <seealso cref="http://www.dev-c.com/nativedb/func/info/0cf2b696bbf945ae"/>
        /// <remarks>
        /// Native Status Codes:
        ///     0 - User still editing
        ///     1 - User has finished editing
        ///     2 - User has canceled editing
        ///     3 - Keyboard isn't active
        /// </remarks>
        internal static bool IsAnyComputerKeyDown(bool rightNow, params Keys[] keysPressed)
        {
            if (!IsKeyboardOpen)
            {
                foreach (Keys key in keysPressed)
                {
                    bool isDown = (rightNow) ? Rage.Game.IsKeyDownRightNow(key) : Rage.Game.IsKeyDown(key);
                    if (isDown)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the specified modifier AND key are both pressed.
        /// </summary>
        /// <param name="mainKey">The primary <see cref="Keys"/> we are checking is pressed.</param>
        /// <param name="modifierKey">The modifier <see cref="Keys"/> we are checking is pressed.</param>
        /// <param name="rightNow">If true, checks if the key is still pressed down during this frame render</param>
        /// <returns></returns>
        internal static bool IsKeyDownWithModifier(Keys mainKey, Keys modifierKey, bool rightNow = false)
        {
            // If no modifier, then just return IsKeyDown
            if (modifierKey == Keys.None)
                return IsComputerKeyDown(mainKey, rightNow, false);

            // Is this a valid modifier key?
            if (!Modifiers.Contains(modifierKey))
                throw new ArgumentException($"Invalid modifier key passed: '{modifierKey}'", nameof(modifierKey));

            // Get on keyboard status
            if (!IsKeyboardOpen && Rage.Game.IsKeyDownRightNow(modifierKey))
            {
                return (rightNow) ? Rage.Game.IsKeyDownRightNow(mainKey) : Rage.Game.IsKeyDown(mainKey);
            }

            return false;
        }

        /// <summary>
        /// Returns whether any modifier key is down right now
        /// </summary>
        /// <returns></returns>
        private static bool IsAnyModifierKeyDownRightNow()
        {
            /*
            foreach (Keys key in Modifiers)
            {
                if (Game.IsKeyDownRightNow(key)) return true;
            }
            */

            return (Rage.Game.IsAltKeyDownRightNow || Rage.Game.IsShiftKeyDownRightNow || Rage.Game.IsControlKeyDownRightNow);
        }
    }
}

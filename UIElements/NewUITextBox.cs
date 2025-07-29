using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser
{
	internal class NewUITextBox : UIPanel//UITextPanel<string>
	{
		private static readonly Asset<Texture2D> CloseButtonTexture = RecipeBrowser.instance.Assets.Request<Texture2D>("UIElements/closeButton", AssetRequestMode.ImmediateLoad);
		internal bool focused = false;

		//private int _cursor;
		//private int _frameCount;
		private int _maxLength = 60;

		private string hintText;
		internal string currentString = "";
		private int textBlinkerCount;
		private int textBlinkerState;

		/// <summary>
		/// Number of characters at the start of <see cref="currentString"/> considered valid.
		/// </summary>
		public int ValidLength { get; private set; }

		/// <summary>
		/// If true, characters beyond <see cref="ValidLength"/> are rendered using <see cref="HighlightColor"/>.
		/// </summary>
		public bool HighlightInvalid { get; set; }

		/// <summary>
		/// Color used to render invalid suffix when <see cref="HighlightInvalid"/> is true.
		/// </summary>
		public Color HighlightColor { get; set; } = Color.Red;

		/// <summary>
		/// Delegate to compute <see cref="ValidLength"/> from <see cref="currentString"/>.
		/// </summary>
		public Func<string, int> ValidLengthResolver { get; set; }

		public event Action OnFocus;

		public event Action OnUnfocus;

		public event Action OnTextChanged;

		public event Action OnTabPressed;

		public event Action OnEnterPressed;

		//public event Action OnUpPressed;
		internal bool unfocusOnEnter = true;

		internal bool unfocusOnTab = true;

		//public NewUITextBox(string text, float textScale = 1, bool large = false) : base("", textScale, large)
		public NewUITextBox(string hintText, string text = "")
		{
			this.hintText = hintText;
			currentString = text;
			SetPadding(0);
			BackgroundColor = Color.White;
			BorderColor = Color.White;
			//			keyBoardInput.newKeyEvent += KeyboardInput_newKeyEvent;

			var closeButton = new UIHoverImageButton(CloseButtonTexture, "");
			closeButton.OnLeftClick += (a, b) => SetText("");
			closeButton.Left.Set(-20f, 1f);
			//closeButton.Top.Set(0f, .5f);
			closeButton.VAlign = 0.5f;
			//closeButton.HAlign = 0.5f;
			Append(closeButton);
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			Focus();
			base.LeftClick(evt);
		}

		public override void RightClick(UIMouseEvent evt)
		{
			base.RightClick(evt);
			SetText("");
		}

		public void SetUnfocusKeys(bool unfocusOnEnter, bool unfocusOnTab)
		{
			this.unfocusOnEnter = unfocusOnEnter;
			this.unfocusOnTab = unfocusOnTab;
		}

		//void KeyboardInput_newKeyEvent(char obj)
		//{
		//	// Problem: keyBoardInput.newKeyEvent only fires on regular keyboard buttons.

		//	if (!focused) return;
		//	if (obj.Equals((char)Keys.Back)) // '\b'
		//	{
		//		Backspace();
		//	}
		//	else if (obj.Equals((char)Keys.Enter))
		//	{
		//		Unfocus();
		//		Main.chatRelease = false;
		//	}
		//	else if (Char.IsLetterOrDigit(obj))
		//	{
		//		Write(obj.ToString());
		//	}
		//}

		public void Unfocus()
		{
			if (focused)
			{
				focused = false;
				Main.blockInput = false;

				OnUnfocus?.Invoke();
			}
		}

		public void Focus()
		{
			if (!focused)
			{
				Main.clrInput();
				focused = true;
				Main.blockInput = true;

				OnFocus?.Invoke();
			}
		}

		public override void Update(GameTime gameTime)
		{
			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (!ContainsPoint(MousePosition) && (Main.mouseLeft || Main.mouseRight)) // This solution is fine, but we need a way to cleanly "unload" a UIElement
			{
				// TODO, figure out how to refocus without triggering unfocus while clicking enable button.
				Unfocus();
			}
			base.Update(gameTime);
		}

		//public void Write(string text)
		//{
		//	base.SetText(base.Text.Insert(this._cursor, text));
		//	this._cursor += text.Length;
		//	_cursor = Math.Min(Text.Length, _cursor);
		//	Recalculate();

		//	OnTextChanged?.Invoke();
		//}

		//public void WriteAll(string text)
		//{
		//	bool changed = text != Text;
		//	if (!changed) return;
		//	base.SetText(text);
		//	this._cursor = text.Length;
		//	//_cursor = Math.Min(Text.Length, _cursor);
		//	Recalculate();

		//	if (changed)
		//	{
		//		OnTextChanged?.Invoke();
		//	}
		//}

		public void SetText(string text)
		{
			if (text.ToString().Length > this._maxLength)
			{
				text = text.ToString().Substring(0, this._maxLength);
			}
			if (currentString != text)
			{
				currentString = text;
				OnTextChanged?.Invoke();
			}
		}

		public void SetTextMaxLength(int maxLength)
		{
			this._maxLength = maxLength;
		}

		//public void Backspace()
		//{
		//	if (this._cursor == 0)
		//	{
		//		return;
		//	}
		//	base.SetText(base.Text.Substring(0, base.Text.Length - 1));
		//	Recalculate();
		//}

		/*public void CursorLeft()
		{
			if (this._cursor == 0)
			{
				return;
			}
			this._cursor--;
		}

		public void CursorRight()
		{
			if (this._cursor < base.Text.Length)
			{
				this._cursor++;
			}
		}*/

		private static bool JustPressed(Keys key)
		{
			return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
		}

		/// <summary>
		/// Calculates the length of the longest prefix of <paramref name="input"/>
		/// that appears in any string returned by <paramref name="textSelector"/>
		/// for items in <paramref name="sourceCollection"/>, ignoring case.
		/// This implementation avoids multiple enumeration by materializing the collection once.
		/// </summary>
		internal int CalculateMaxValidLength<T>(
			string input,
			Func<T, string> textSelector,
			IEnumerable<T> sourceCollection
		)
		{
			if (string.IsNullOrEmpty(input))
			{
				return 0;
			}

			var items = sourceCollection as IList<T> ?? sourceCollection.ToList();
			int maxValid = 0;
			
			for (int len = 1; len <= input.Length; len++)
			{
				string prefix = input[..len];
				bool found = items.Any(item => textSelector(item).Contains(prefix, StringComparison.OrdinalIgnoreCase));

				if (found)
				{
					maxValid = len;
				}
				else
				{
					break;
				}
			}

			return maxValid;
		}

		/// <summary>
		/// Recomputes <see cref="ValidLength"/> using <see cref="ValidLengthResolver"/>
		/// if <see cref="HighlightInvalid"/> is enabled; otherwise treats the entire string as valid.
		/// </summary>
		private void RecomputeValidLength()
		{
			if (ValidLengthResolver != null && HighlightInvalid)
			{
				ValidLength = ValidLengthResolver(currentString);
			}
			else
			{
				ValidLength = currentString.Length;
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle hitbox = GetInnerDimensions().ToRectangle();

			// Draw panel
			base.DrawSelf(spriteBatch);
			//	Main.spriteBatch.Draw(Main.magicPixel, hitbox, Color.Yellow);

			if (focused)
			{
				Terraria.GameInput.PlayerInput.WritingText = true;
				Main.instance.HandleIME();
				string newString = Main.GetInputText(currentString);
				if (!newString.Equals(currentString))
				{
					currentString = newString;
					RecomputeValidLength();
					OnTextChanged?.Invoke();
				}
				else
				{
					currentString = newString;
				}

				if (JustPressed(Keys.Tab))
				{
					if (unfocusOnTab) Unfocus();
					OnTabPressed?.Invoke();
				}
				if (JustPressed(Keys.Enter))
				{
					Main.drawingPlayerChat = false;
					if (unfocusOnEnter) Unfocus();
					OnEnterPressed?.Invoke();
				}
				if (++textBlinkerCount >= 20)
				{
					textBlinkerState = (textBlinkerState + 1) % 2;
					textBlinkerCount = 0;
				}
				Main.instance.DrawWindowsIMEPanel(new Vector2(98f, (float)(Main.screenHeight - 36)), 0f);
			}
			string displayString = currentString;
			if (this.textBlinkerState == 1 && focused)
			{
				displayString = displayString + "|";
			}
			CalculatedStyle space = base.GetDimensions();
			Color color = Color.Black;
			if (currentString.Length == 0)
			{
			}
			Vector2 drawPos = space.Position() + new Vector2(4, 2);
			if (currentString.Length == 0 && !focused)
			{
				color *= 0.5f;
				spriteBatch.DrawString(FontAssets.MouseText.Value, hintText, drawPos, color);
			}
			else
			{
				//Utils.DrawBorderString(spriteBatch, displayString, drawPos, Color.White, 1f);
				// spriteBatch.DrawString(FontAssets.MouseText.Value, displayString, drawPos, color);
				
				if (!HighlightInvalid || ValidLength >= currentString.Length)
					ValidLength = currentString.Length;
				
				string valid = currentString[..ValidLength];
				string invalid = HighlightInvalid
					? currentString[ValidLength..]
					: "";
				
				float validWidth = FontAssets.MouseText.Value.MeasureString(valid).X;
				float invalidWidth = FontAssets.MouseText.Value.MeasureString(invalid).X;
				
				spriteBatch.DrawString(FontAssets.MouseText.Value, valid, drawPos, color);
				
				if (invalid.Length > 0)
				{
					spriteBatch.DrawString(
						FontAssets.MouseText.Value,
						invalid,
						drawPos + new Vector2(validWidth, 0),
						HighlightColor
					);
				}
				
				if (focused && textBlinkerState == 1)
				{
					spriteBatch.DrawString(
						FontAssets.MouseText.Value,
						"|",
						drawPos + new Vector2(validWidth + invalidWidth, 0),
						color
					);
				}
			}

			//			CalculatedStyle innerDimensions2 = base.GetInnerDimensions();
			//			Vector2 pos2 = innerDimensions2.Position();
			//			if (IsLarge)
			//			{
			//				pos2.Y -= 10f * TextScale * TextScale;
			//			}
			//			else
			//			{
			//				pos2.Y -= 2f * TextScale;
			//			}
			//			//pos2.X += (innerDimensions2.Width - TextSize.X) * 0.5f;
			//			if (IsLarge)
			//			{
			//				Utils.DrawBorderStringBig(spriteBatch, Text, pos2, TextColor, TextScale, 0f, 0f, -1);
			//				return;
			//			}
			//			Utils.DrawBorderString(spriteBatch, Text, pos2, TextColor, TextScale, 0f, 0f, -1);
			//
			//			this._frameCount++;
			//
			//			CalculatedStyle innerDimensions = base.GetInnerDimensions();
			//			Vector2 pos = innerDimensions.Position();
			//			DynamicSpriteFont spriteFont = base.IsLarge ? Main.fontDeathText : FontAssets.MouseText.Value;
			//			Vector2 vector = new Vector2(spriteFont.MeasureString(base.Text.Substring(0, this._cursor)).X, base.IsLarge ? 32f : 16f) * base.TextScale;
			//			if (base.IsLarge)
			//			{
			//				pos.Y -= 8f * base.TextScale;
			//			}
			//			else
			//			{
			//				pos.Y -= 1f * base.TextScale;
			//			}
			//			if (Text.Length == 0)
			//			{
			//				Vector2 hintTextSize = new Vector2(spriteFont.MeasureString(hintText.ToString()).X, IsLarge ? 32f : 16f) * TextScale;
			//				pos.X += 5;//(hintTextSize.X);
			//				if (base.IsLarge)
			//				{
			//					Utils.DrawBorderStringBig(spriteBatch, hintText, pos, Color.Gray, base.TextScale, 0f, 0f, -1);
			//					return;
			//				}
			//				Utils.DrawBorderString(spriteBatch, hintText, pos, Color.Gray, base.TextScale, 0f, 0f, -1);
			//				pos.X -= 5;
			//				//pos.X -= (innerDimensions.Width - hintTextSize.X) * 0.5f;
			//			}
			//
			//			if (!focused) return;
			//
			//			pos.X += /*(innerDimensions.Width - base.TextSize.X) * 0.5f*/ +vector.X - (base.IsLarge ? 8f : 4f) * base.TextScale + 6f;
			//			if ((this._frameCount %= 40) > 20)
			//			{
			//				return;
			//			}
			//			if (base.IsLarge)
			//			{
			//				Utils.DrawBorderStringBig(spriteBatch, "|", pos, base.TextColor, base.TextScale, 0f, 0f, -1);
			//				return;
			//			}
			//			Utils.DrawBorderString(spriteBatch, "|", pos, base.TextColor, base.TextScale, 0f, 0f, -1);
		}
	}
}

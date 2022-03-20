﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Project1.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Project1.Components
{
    class DialogBox
    {
        /// <summary>
        /// All text contained in this dialog box
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Bool that determines active state of this dialog box
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// X,Y coordinates of this dialog box
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Width and Height of this dialog box
        /// </summary>
        public Vector2 Size { get; set; }

        /// <summary>
        /// Color used to fill dialog box background
        /// </summary>
        public Color FillColor { get; set; }

        /// <summary>
        /// Color used for border around dialog box
        /// </summary>
        public Color BorderColor { get; set; }

        /// <summary>
        /// Color used for text in dialog box
        /// </summary>
        public Color DialogColor { get; set; }

        private Game1 game_;
        private SpriteFont font_;

        /// <summary>
        /// Thickness of border
        /// </summary>
        public int BorderWidth { get; set; }

        /// <summary>
        /// Background fill texture (built from FillColor)
        /// </summary>
        private readonly Texture2D fill_texture_;

        /// <summary>
        /// Border fill texture (built from BorderColor)
        /// </summary>
        private readonly Texture2D border_texture_;

        /// <summary>
        /// Collection of pages contained in this dialog box
        /// </summary>
        private List<string> pages_;

        /// <summary>
        /// Margin surrounding the text inside the dialog box
        /// </summary>
        private const float DialogBoxMargin = 24f;

        /// <summary>
        /// Size (in pixels) of a wide alphabet letter (W is the widest letter in almost every font) 
        /// </summary>
        private Vector2 character_size_;
        private KeyboardManager keyboard_;

        /// <summary>
        /// The amount of characters allowed on a given line
        /// NOTE: If you want to use a font that is not monospaced, this will need to be reevaluated
        /// </summary>
        private int MaxCharsPerLine => (int)Math.Floor((Size.X - DialogBoxMargin) / character_size_.X);

        /// <summary>
        /// Determine the maximum amount of lines allowed per page
        /// NOTE: This will change automatically with font size
        /// </summary>
        private int MaxLines => (int)Math.Floor((Size.Y - DialogBoxMargin) / character_size_.Y) - 1;

        /// <summary>
        /// The index of the current page
        /// </summary>
        private int current_page_;

        /// <summary>
        /// The stopwatch interval (used for blinking indicator)
        /// </summary>
        private int interval_;

        /// <summary>
        /// The position and size of the dialog box fill rectangle
        /// </summary>
        private Rectangle TextRectangle => new Rectangle(Position.ToPoint(), Size.ToPoint());

        /// <summary>
        /// The position and size of the bordering sides on the edges of the dialog box
        /// </summary>
        private List<Rectangle> BorderRectangles => new List<Rectangle>
        {
            // Top (contains top-left & top-right corners)
            new Rectangle(TextRectangle.X - BorderWidth, TextRectangle.Y - BorderWidth,
                TextRectangle.Width + BorderWidth*2, BorderWidth),

            // Right
            new Rectangle(TextRectangle.X + TextRectangle.Size.X, TextRectangle.Y, BorderWidth, TextRectangle.Height),

            // Bottom (contains bottom-left & bottom-right corners)
            new Rectangle(TextRectangle.X - BorderWidth, TextRectangle.Y + TextRectangle.Size.Y,
                TextRectangle.Width + BorderWidth*2, BorderWidth),

            // Left
            new Rectangle(TextRectangle.X - BorderWidth, TextRectangle.Y, BorderWidth, TextRectangle.Height)
        };

        /// <summary>
        /// The starting position of the text inside the dialog box
        /// </summary>
        private Vector2 TextPosition => new Vector2(Position.X + DialogBoxMargin / 2, Position.Y + DialogBoxMargin / 2);

        /// <summary>
        /// Stopwatch used for the blinking (next page) indicator
        /// </summary>
        private Stopwatch stopwatch_;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DialogBox(Game1 game, SpriteFont font)
        {
            game_ = game;

            BorderWidth = 2;
            DialogColor = Color.Black;

            FillColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

            BorderColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

            fill_texture_ = new Texture2D(game_.GraphicsDevice, 1, 1);
            fill_texture_.SetData(new[] { FillColor });

            border_texture_ = new Texture2D(game_.GraphicsDevice, 1, 1);
            border_texture_.SetData(new[] { BorderColor });

            pages_ = new List<string>();
            current_page_ = 0;

            var sizeX = (int)(game_.GraphicsDevice.Viewport.Width * 0.5);
            var sizeY = (int)(game_.GraphicsDevice.Viewport.Height * 0.2);

            Size = new Vector2(sizeX, sizeY);

            var posX = game_.CenterScreen.X - (Size.X / 2f);
            var posY = game_.GraphicsDevice.Viewport.Height - Size.Y - 30;

            Position = new Vector2(posX, posY);

            font_ = font;
            character_size_ = font_.MeasureString(new StringBuilder("W", 1));

            keyboard_ = new KeyboardManager();
        }

        /// <summary>
        /// Initialize a dialog box
        /// - can be used to reset the current dialog box in case of "I didn't quite get that..."
        /// </summary>
        /// <param name="text"></param>
        public void Initialize(string text = null)
        {
            Text = text ?? Text;

            current_page_ = 0;

            Show();
        }

        /// <summary>
        /// Show the dialog box on screen
        /// - invoke this method manually if Text changes
        /// </summary>
        public void Show()
        {
            Active = true;

            // use stopwatch to manage blinking indicator
            stopwatch_ = new Stopwatch();

            stopwatch_.Start();

            pages_ = WordWrap(Text);
        }

        /// <summary>
        /// Manually hide the dialog box
        /// </summary>
        public void Hide()
        {
            Active = false;

            stopwatch_.Stop();

            stopwatch_ = null;
        }

        /// <summary>
        /// Process input for dialog box
        /// </summary>
        public void Update()
        {
            if (Active)
            {
                KeyboardManager.GetState();
                // Button press will proceed to the next page of the dialog box
                if (KeyboardManager.HasBeenPressed(Keys.Enter))// && Keyboard.GetState().IsKeyUp(Keys.Enter)))
                {
                    if (current_page_ >= pages_.Count - 1)
                    {
                        Hide();
                    }
                    else
                    {
                        current_page_++;
                        stopwatch_.Restart();
                    }
                }

                // Shortcut button to skip entire dialog box
                if (KeyboardManager.HasBeenPressed(Keys.X))
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// Draw the dialog box on screen if it's currently active
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (Active)
            {
                // Draw each side of the border rectangle
                foreach (var side in BorderRectangles)
                {
                    spriteBatch.Draw(border_texture_, side, Color.White * 0.5f);
                }

                // Draw background fill texture (in this example, it's 50% transparent white)
                spriteBatch.Draw(fill_texture_, TextRectangle, Color.White * 0.5f);

                // Draw the current page onto the dialog box
                spriteBatch.DrawString(font_, pages_[current_page_], TextPosition, DialogColor);

                // Draw a blinking indicator to guide the player through to the next page
                // This stops blinking on the last page
                // NOTE: You probably want to use an image here instead of a string
                if (BlinkIndicator() || current_page_ == pages_.Count - 1)
                {
                    var indicatorPosition = new Vector2(TextRectangle.X + TextRectangle.Width - (character_size_.X) - 4,
                        TextRectangle.Y + TextRectangle.Height - (character_size_.Y));

                    spriteBatch.DrawString(font_, ">", indicatorPosition, Color.Red);
                }
            }
        }

        /// <summary>
        /// Whether the indicator should be visible or not
        /// </summary>
        /// <returns></returns>
        private bool BlinkIndicator()
        {
            interval_ = (int)Math.Floor((double)(stopwatch_.ElapsedMilliseconds % 1000));

            return interval_ < 500;
        }

        /// <summary>
        /// Wrap words to the next line where applicable
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private List<string> WordWrap(string text)
        {
            var pages = new List<string>();

            var capacity = MaxCharsPerLine * MaxLines > text.Length ? text.Length : MaxCharsPerLine * MaxLines;

            var result = new StringBuilder(capacity);
            var resultLines = 0;

            var currentWord = new StringBuilder();
            var currentLine = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var currentChar = text[i];
                var isNewLine = text[i] == '\n';
                var isLastChar = i == text.Length - 1;

                currentWord.Append(currentChar);

                if (char.IsWhiteSpace(currentChar) || isLastChar)
                {
                    var potentialLength = currentLine.Length + currentWord.Length;

                    if (potentialLength > MaxCharsPerLine)
                    {
                        result.AppendLine(currentLine.ToString());

                        currentLine.Clear();

                        resultLines++;
                    }

                    currentLine.Append(currentWord);

                    currentWord.Clear();

                    if (isLastChar || isNewLine)
                    {
                        result.AppendLine(currentLine.ToString());
                    }

                    if (resultLines > MaxLines || isLastChar || isNewLine)
                    {
                        pages.Add(result.ToString());

                        result.Clear();

                        resultLines = 0;

                        if (isNewLine)
                        {
                            currentLine.Clear();
                        }
                    }
                }
            }

            return pages;
        }
    }
}

//------------------------------------------------------------------------------
//      Copyright (c) Microsoft Corporation.  All rights reserved.                                                               
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace Terrarium.Glass
{
	public sealed class GlassHelper
	{
        private GlassHelper(){}

        static GlassHelper()
        {
            //Bitmap bitmap = new Bitmap(128, 96);
            //Graphics graphics = Graphics.FromImage(bitmap);
            //Font font = new Font("Verdana", 16, FontStyle.Regular);
            //SizeF stringSize = graphics.MeasureString("Whidbey Beta 1", font);
            //graphics.Dispose();
            //bitmap.Dispose();

            //bitmap = new Bitmap((int)(2.0f*stringSize.Width), (int)(2.0f * stringSize.Height));
            //graphics = Graphics.FromImage(bitmap);

            //Brush fontBrush = new SolidBrush(Color.FromArgb(16, 0, 0, 0));

            //graphics.DrawString("Whidbey Beta 2", font, fontBrush, 0, 0);
            //graphics.DrawString("Whidbey Beta 2", font, fontBrush, (int)(1.0f*stringSize.Width), (int)(1.0f*stringSize.Height));
            //fontBrush.Dispose();

            //font.Dispose();
            //graphics.Dispose();

            //bitmap.Dispose();
        }

		public static void FillRectangle(Rectangle rectangle, GlassGradient gradient, bool isSunk, bool isGlass, Graphics graphics)
		{
            if (rectangle.Width == 0 || rectangle.Height == 0)
                return;

			GlassGradient activeGradient = null;
			if (isSunk == true)
				activeGradient = new GlassGradient(gradient.Bottom, gradient.Top);
			else
				activeGradient = new GlassGradient(gradient.Top, gradient.Bottom);

			Brush brush = activeGradient.GetBrush(rectangle);
			graphics.FillRectangle(brush, rectangle);
			brush.Dispose();

			if (isGlass == true)
			{
				graphics.SetClip(rectangle);

				Rectangle glassRectangle;

				if (isSunk == true)
					glassRectangle = new Rectangle(new Point(rectangle.Left, rectangle.Height / 2), new Size(rectangle.Width, rectangle.Height / 2));
				else
					glassRectangle = new Rectangle(rectangle.Location, new Size(rectangle.Width, rectangle.Height / 2));

				Brush glassBrush = new LinearGradientBrush(glassRectangle, Color.Transparent, Color.FromArgb(32, 255, 255, 255), 90.0f);

				graphics.FillRectangle(glassBrush, glassRectangle);

				glassBrush.Dispose();
			}
		}

		public static void FillRoundedRectangle(Rectangle rectangle, GlassGradient gradient, bool isSunk, bool isGlass, Graphics graphics)
		{
            if (rectangle.Width == 0 || rectangle.Height == 0)
                return;
            
            GraphicsPath path = GetRoundedRectanglePath(rectangle);

			GlassGradient activeGradient = null; 
			if (isSunk == true)
				activeGradient = new GlassGradient(gradient.Bottom, gradient.Top);
			else
				activeGradient = new GlassGradient(gradient.Top, gradient.Bottom);

			Brush brush = activeGradient.GetBrush(rectangle);
			graphics.FillPath(brush, path);
			brush.Dispose();

			if (isGlass == true)
			{
				graphics.SetClip(path);

				Rectangle glassRectangle ;//= new Rectangle(rectangle.Location, new Size(rectangle.Width, rectangle.Height / 2));
				if (isSunk == true)
					glassRectangle = new Rectangle(new Point(rectangle.Left, rectangle.Height / 2), new Size(rectangle.Width, rectangle.Height / 2));
				else
					glassRectangle = new Rectangle(rectangle.Location, new Size(rectangle.Width, rectangle.Height / 2));
				
				GraphicsPath glassPath = GetRoundedRectanglePath(glassRectangle);
				Brush glassBrush = new LinearGradientBrush(glassRectangle, Color.Transparent, Color.FromArgb(32, 255, 255, 255), 90.0f);

				graphics.FillPath(glassBrush, glassPath);

				glassBrush.Dispose();
				glassPath.Dispose();
			}
		}

		public static void DrawGlass(Rectangle rectangle, Graphics graphics)
		{
            if (rectangle.Width == 0 || rectangle.Height == 0)
                return;
            
            GraphicsPath clipPath = GetRoundedRectanglePath(rectangle);

			graphics.SetClip(clipPath);

			Rectangle glassRectangle = new Rectangle(rectangle.Location, new Size(rectangle.Width, rectangle.Height / 2));

			// Apply a glass look
			Brush glassBrush = new LinearGradientBrush(glassRectangle, Color.Transparent, Color.FromArgb(32, 255, 255, 255), 90.0f);
			//Brush glassBrush = new LinearGradientBrush(glassRectangle, Color.Red, Color.Blue, 90.0f);

			GraphicsPath glassPath = GetRoundedRectanglePath(glassRectangle);

			graphics.FillPath(glassBrush, glassPath);

			glassPath.Dispose();
			glassBrush.Dispose();

			glassRectangle = new Rectangle(0, rectangle.Height - (rectangle.Height / 4), rectangle.Width, rectangle.Height * 3);
			glassPath = GetRoundedRectanglePath(glassRectangle);
			glassBrush = new SolidBrush(Color.FromArgb(16, Color.White));
			
			//graphics.FillPath(glassBrush,glassPath);

			glassBrush.Dispose();
			glassPath.Dispose();

			graphics.SetClip(rectangle);
			clipPath.Dispose();
		}

		public static void DrawRoundedGradient(Rectangle rectangle, GlassGradient gradient, Graphics graphics)
		{
            if (rectangle.Width == 0 || rectangle.Height == 0)
                return;

			Brush brush = gradient.GetBrush(rectangle);
			GraphicsPath path = GetRoundedRectanglePath(rectangle);

			graphics.FillPath(brush, path);

			path.Dispose();
			brush.Dispose();
		}

		public static void FillTexturedRectangle(Rectangle rectangle, Image texture, bool drawGradient, Graphics graphics)
		{
            if (rectangle.Width == 0 || rectangle.Height == 0)
                return;
            
            TextureBrush textureBrush = new TextureBrush(texture, WrapMode.Tile);
			graphics.FillRectangle(textureBrush, rectangle);
			textureBrush.Dispose();

			if (drawGradient == true)
			{
				Brush gradientBrush = new LinearGradientBrush(rectangle, Color.FromArgb(96, Color.White), Color.FromArgb(160, Color.Black), 90.0f);
				graphics.FillRectangle(gradientBrush, rectangle);
				gradientBrush.Dispose();
			}
		}


		public static void DrawRoundedBorder(Rectangle rectangle, Color color, float width, Graphics graphics)
		{
			if (rectangle.Width == 0 || rectangle.Height == 0)
				return;

			Pen pen = new Pen(color, width);
			GraphicsPath path = GetRoundedRectanglePath(rectangle);

			graphics.DrawPath(pen, path);

			path.Dispose();
			pen.Dispose();
		}

		public static void DrawBorder( Rectangle rectangle, GlassBorders borders, Graphics graphics )
		{
			if ( rectangle.Width == 0 || rectangle.Height == 0 )
				return;

			Pen borderPen = new Pen( GlassStyleManager.Active.BorderColor );

			if ( (borders & GlassBorders.Left) > 0 )
				graphics.DrawLine( borderPen, 0, 0, 0, rectangle.Height - 1 );
			if ( (borders & GlassBorders.Top) > 0 )
				graphics.DrawLine( borderPen, 0, 0, rectangle.Width - 1, 0 );
			if ( (borders & GlassBorders.Right) > 0 )
				graphics.DrawLine( borderPen, rectangle.Width - 1, 0, rectangle.Width - 1, rectangle.Height - 1 );
			if ( (borders & GlassBorders.Bottom) > 0 )
				graphics.DrawLine( borderPen, 0, rectangle.Height - 1, rectangle.Width - 1, rectangle.Height - 1 );

			borderPen.Dispose();

		}

		public static void DrawText( string text, Rectangle rectangle, ContentAlignment contentAlignment, Graphics graphics, bool noWrap, Color foreColor, Color shadowColor )
		{
            if (rectangle.Width == 0 || rectangle.Height == 0)
                return;
            
            StringFormat stringFormat = new StringFormat();

			switch( contentAlignment )
			{
				case ContentAlignment.BottomCenter:
				{
					stringFormat.Alignment = StringAlignment.Center;
					stringFormat.LineAlignment = StringAlignment.Far;
					break;
				}
				case ContentAlignment.BottomLeft:
				{
					stringFormat.Alignment = StringAlignment.Near;
					stringFormat.LineAlignment = StringAlignment.Far;
					break;
				}
				case ContentAlignment.BottomRight:
				{
					stringFormat.Alignment = StringAlignment.Far;
					stringFormat.LineAlignment = StringAlignment.Far;
					break;
				}

				case ContentAlignment.MiddleCenter:
				{
					stringFormat.Alignment = StringAlignment.Center;
					stringFormat.LineAlignment = StringAlignment.Center;
					break;
				}
				case ContentAlignment.MiddleLeft:
				{
					stringFormat.Alignment = StringAlignment.Near;
					stringFormat.LineAlignment = StringAlignment.Center;
					break;
				}
				case ContentAlignment.MiddleRight:
				{
					stringFormat.Alignment = StringAlignment.Far;
					stringFormat.LineAlignment = StringAlignment.Center;
					break;
				}

				case ContentAlignment.TopCenter:
				{
					stringFormat.Alignment = StringAlignment.Center;
					stringFormat.LineAlignment = StringAlignment.Near;
					break;
				}
				case ContentAlignment.TopLeft:
				{
					stringFormat.Alignment = StringAlignment.Near;
					stringFormat.LineAlignment = StringAlignment.Near;
					break;
				}
				case ContentAlignment.TopRight:
				{
					stringFormat.Alignment = StringAlignment.Far;
					stringFormat.LineAlignment = StringAlignment.Near;
					break;
				}
			}

			if ( noWrap == true )
				stringFormat.FormatFlags |= StringFormatFlags.NoWrap;

			stringFormat.Trimming = StringTrimming.EllipsisCharacter;

			if ( GlassStyleManager.Active.FontShadow == true )
			{
				RectangleF shadowRectangle = new RectangleF( rectangle.X+1, rectangle.Y+1, rectangle.Width, rectangle.Height );
				
				SolidBrush shadowBrush = new SolidBrush( shadowColor == Color.Empty ? Color.Black : shadowColor );
	
				graphics.DrawString( text, GlassStyleManager.Active.Font, shadowBrush, shadowRectangle, stringFormat );

				shadowBrush.Dispose();
				shadowBrush = null;
			}

			RectangleF normalRectangle = new RectangleF( rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height );

			SolidBrush fontBrush = new SolidBrush( foreColor == Color.Empty ? GlassStyleManager.Active.ForeColor : foreColor );

			graphics.DrawString( text, GlassStyleManager.Active.Font, fontBrush, normalRectangle, stringFormat );

			fontBrush.Dispose();
		}

		private static GraphicsPath GetRoundedRectanglePath(Rectangle rect)
		{

			GraphicsPath path = new GraphicsPath();

			try
			{
				if (rect.Width > rect.Height)
				{
					Rectangle rectangleArc = new Rectangle(rect.Left, rect.Top, rect.Height, rect.Height);
					path.AddArc(rectangleArc, 90, 180);
					rectangleArc.X = rect.Right - rect.Height;
					path.AddArc(rectangleArc, 270, 180);
				}
				else if (rect.Width < rect.Height)
				{
					Rectangle rectangleArc = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Width);
					path.AddArc(rectangleArc, 180, 180);
					rectangleArc.Y = (rect.Bottom-1) - rect.Width;
					path.AddArc(rectangleArc, 0, 180);
				}
				else
				{
					path.AddEllipse(rect);
				}
			}
			catch
			{
				path.AddEllipse(rect);
			}

			path.CloseFigure();

			return path;
		}


	}
}

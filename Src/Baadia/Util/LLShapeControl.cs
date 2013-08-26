﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using Loyc;
using Loyc.Collections;
using System.ComponentModel;
using Loyc.Geometry;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace Util.WinForms
{
	/// <summary>A control that draws <see cref="LLShape"/> objects on its surface.</summary>
	/// <remarks>
	/// This class provides a convenient way to draw custom controls. It consists
	/// of one or more layers (<see cref="LLShapeLayer"/>, and each layer contains 
	/// a list of shapes of type <see cref="LLShape"/>.
	/// <para/>
	/// It is recommended to use only a couple of layers in most cases,
	/// because compositing the layers can be a major cost by itself.
	/// </remarks>
	public class LLShapeControl : Control
	{
		public LLShapeControl()
		{
			BackColor = Color.White;
			_layers.ListChanging += (sender, e) => { Invalidate(); };
			AddLayer(false);
		}

		AList<LLShapeLayer> _layers = new AList<LLShapeLayer>();
		public IListSource<LLShapeLayer> Layers { get { return _layers; } }

		/// <summary>Initializes a new LLShapeLayer.</summary>
		/// <param name="useAlpha">Whether the backing bitmap should have an alpha 
		/// channel, see <see cref="LLShapeLayer"/> for more information.</param>
		public virtual LLShapeLayer AddLayer(bool? useAlpha = null)
		{
			if (_layers.Count == 0 && useAlpha == null)
				useAlpha = false;
			var layer = new LLShapeLayer(this, useAlpha);
			layer.Resize(Width, Height);
			_layers.Add(layer);
			return layer;
		}
		public virtual LLShapeLayer InsertLayer(int index, bool? useAlpha = null)
		{
			if (index == 0 && useAlpha == null)
				useAlpha = false;
			var layer = new LLShapeLayer(this, useAlpha);
			layer.Resize(Width, Height);
			_layers.Insert(index, layer);
			return layer;
		}
		public virtual void RemoveLayerAt(int index)
		{
			_layers[index].Dispose();
			_layers.RemoveAt(index);
		}

		Bitmap _completeFrame;
		bool _suspendDraw, _needRedraw, _drawPending;
				
		/// <summary>Temporarily blocks automatic redrawing.</summary>
		/// <remarks>It's useful to call this method when the control is hidden
		/// or shown in an off-screen tab or window, so that unnecessary drawing
		/// does not happen. The suspension is automatically cancelled when a
		/// repaint request is received, so you don't generally have to call
		/// <see cref="ResumeDrawing"/>.</remarks>
		public void SuspendDrawing() { _suspendDraw = true; }
		/// <summary>If drawing is suspended, this cancel the suspension.</summary>
		/// <seealso cref="SuspendDrawing"/>
		public void ResumeDrawing() { _suspendDraw = false; AutoDrawLayers(); }

		public new void Invalidate(bool invalidateLayers = false)
		{
			_needRedraw = true;
			AutoDrawLayers();
			if (invalidateLayers)
				foreach (var layer in _layers)
					layer.Invalidate();
			base.Invalidate();
		}

		private void AutoDrawLayers()
		{
			if (!_drawPending && _needRedraw && !_suspendDraw) {
				var _ = Handle; // ensure handle exists, so we can use BeginInvoke 
				_drawPending = true;
				BeginInvoke(new Action(DrawLayers));
			}
		}
		
		protected bool IsDesignTime { get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; } }
		static DrawStyle _designStyle;

		private void DrawLayers()
		{
			if (IsDesignTime) {
				_designStyle = _designStyle ?? new DrawStyle();
				_designStyle.Font = Font;
				_designStyle.TextColor = ForeColor;
				if (_layers.Count != 0 && _layers.All(l => l.Shapes.Count == 0))
					_layers[0].Shapes.Add(new LLTextShape(_designStyle, GetType().Name, null, new Point<float>(3, 3)));
			}

			Bitmap combined = null;
			for (int i = 0; i < _layers.Count; i++)
			{
				LLShapeLayer layer = _layers[i];
				var bmp = layer.AutoDraw(combined);
				if (layer.UseAlpha && bmp != combined) {
					// Blit this layer onto the previous one
					if (combined == null) {
						// oops, bottom layer has an alpha channel
						combined = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
						var g_ = Graphics.FromImage(combined);
						g_.Clear(BackColor);
						g_.Dispose();
					}
					var g = Graphics.FromImage(combined);
					g.DrawImage(bmp, new Point());
					g.Dispose();
				} else
					combined = bmp;
			}
			_completeFrame = combined;
			_needRedraw = _drawPending = false;
			base.Invalidate();
		}
		protected override void OnResize(EventArgs e)
		{
			foreach (var layer in _layers)
				layer.Resize(Width, Height);
		}
		protected override void OnPaint(PaintEventArgs pe)
		{
			ResumeDrawing();

			var g = pe.Graphics;
			if (_completeFrame != null)
				g.DrawImage(_completeFrame, new Point());
			else
				base.OnPaint(pe);
		}
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
		}
	}

	/// <summary>One layer within an <see cref="LLShapeControl"/>.</summary>
	/// <remarks>
	/// An <see cref="LLShapeControl"/> can be composed of multiple layers,
	/// each of which can potentially redraw itself independently. Each layer can
	/// be created with or without an alpha channel, which affects performance.
	/// <para/>
	/// Having an alpha channel provides independence from the layer
	/// underneath--if the layer underneath changes, a layer with an alpha channel
	/// will not have to be redrawn. Thus, if the layer will have a lot of graphics 
	/// on it and/or rarely changes, it is recommended to use an alpha channel so 
	/// that the layer will not have to be redrawn when it hasn't changed.
	/// <para/>
	/// When there is no alpha channel, the layer is drawn by duplicating the
	/// layer underneath and drawing on top of that. In some circumstances this
	/// will improve performance by eliminating the need for a slower
	/// compositing step. So, an alpha channel should be not be used when the
	/// layer contains very little graphics (in that case, redrawing is cheap)
	/// or when <i>all</i> the layers underneath rarely change. Also, the alpha
	/// channel should be disabled on the first layer.
	/// <para/>
	/// When calling <see cref="LLShapeControl.Add"/>, you can specify 
	/// <c>useAlpha: null</c> to decide automatically based on the number of shapes
	/// in the layer.
	/// <para/>
	/// Performance tip: drawing and compositing are both skipped when a layer is 
	/// empty, except when drawing the lowest layer. So it's fairly harmless to 
	/// define an extra layer that is usually empty.
	/// </remarks>
	public class LLShapeLayer : IDisposable
	{
		Bitmap _bmp;
		bool? _useAlpha;
		int _width, _height;
		bool _invalid;
		LLShapeControl _container;

		MSet<LLShape> _shapes = new MSet<LLShape>();
		public MSet<LLShape> Shapes { get { return _shapes; } }

		/// <summary>Initializes a new LLShapeLayer.</summary>
		/// <param name="useAlpha">Whether the backing bitmap should have an alpha channel.</param>
		internal LLShapeLayer(LLShapeControl container, bool? useAlpha = null)
		{
			_container = container;
			_useAlpha = useAlpha;
			//_shapes.ListChanging += (sender, e) => { _invalid = true; };
		}
		/// <summary>Resizes the layer's viewport.</summary>
		public void Resize(int width, int height)
		{
			if (_width != width || _height != height) {
				_width = width;
				_height = height;
				Invalidate();
			}
		}
		public void Invalidate()
		{
			_invalid = true;
			_container.Invalidate();
		}
		public bool IsInvalidated
		{
			get { return _invalid || _bmp == null || _bmp.Width != _width || _bmp.Height != _height; }
		}
		public bool UseAlpha
		{
			get { return _useAlpha == true || (_useAlpha == null && Shapes.Count >= 12); }
		}
		public Bitmap AutoDraw(Bitmap lowerLevel)
		{
			bool useAlpha = UseAlpha;
			if (!useAlpha || IsInvalidated)
			{
				_invalid = false;
				
				if (_shapes.Count == 0 && lowerLevel != null)
					return lowerLevel;
				
				var pixFmt = useAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
				if (IsInvalidated || _bmp.PixelFormat != pixFmt) {
					if (_bmp != null)
						_bmp.Dispose();
					_bmp = new Bitmap(_width, _height, pixFmt);
				}

				var g = Graphics.FromImage(_bmp);
				g.SmoothingMode = SmoothingMode.AntiAlias;
				if (useAlpha)
					g.Clear(Color.FromArgb(0, _container.BackColor));
				else if (lowerLevel == null)
					g.Clear(_container.BackColor);
				else
					g.DrawImage(lowerLevel, new Point(0, 0));

				var shapes = new List<Pair<LLShape, Matrix>>();
				foreach (LLShape shape in _shapes)
					Add(shape, shapes, null);
				shapes.Sort((p1,p2) => p1.A.CompareTo(p2.A));

				Matrix curMatrix = null, oldMatrix = g.Transform;
				foreach (var pair in shapes) {
					Debug.Assert(pair.A.IsVisible);
					var matrix = pair.B ?? oldMatrix;
					if (curMatrix != matrix) {
						curMatrix = matrix;
						g.Transform = matrix;
					}
					pair.A.Draw(g);
				}
			}
			return _bmp;
		}

		private void Add(LLShape shape, List<Pair<LLShape, Matrix>> shapes, Matrix matrix)
		{
			if (!shape.IsVisible)
				return;
			var group = shape as LLShapeGroup;
			if (group != null) {
				matrix = CombineMatrices(matrix, group.Transform);
				foreach (var subshape in group.Shapes)
					Add(subshape, shapes, matrix);
			} else {
				shapes.Add(Pair.Create(shape, matrix));
			}
		}
		static Matrix CombineMatrices(Matrix a, Matrix b)
		{
			if (a == null) return b;
			if (b == null) return a;
			var c = a.Clone();
			c.Multiply(b);
			return c;
		}

		public void Dispose()
		{
			if (_bmp != null) {
				_bmp.Dispose();
				_bmp = null;
			}
		}
	}
}
﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Nez
{
	public class Camera
	{
		#region Fields and Properties

		protected Vector2 _position;
		public Vector2 position
		{
			get { return _position; }
			set
			{
				if( _position != value )
				{
					_position = value;
					forceMatrixUpdate();
				}
			}
		}

		protected Vector2 _origin;
		public Vector2 origin
		{
			get { return _origin; }
			set
			{
				if( _origin != value )
				{
					_origin = value;
					forceMatrixUpdate();
				}
			}
		}

		protected float _rotation;
		public float rotation
		{
			get { return _rotation; }
			set
			{
				if( _rotation != value )
				{
					_rotation = value;
					forceMatrixUpdate();
				}
			}
		}

		/// <summary>
		/// the zoom value should be between -1 and 1. This value is then translated to be from minimumZoom to maximumZoom. This lets you set
		/// appropriate minimum/maximum values then use a more intuitive -1 to 1 mapping to change the zoom.
		/// </summary>
		/// <value>The zoom.</value>
		public float zoom
		{
			get
			{
				if( _zoom == 0 )
					return 1f;
				else if( _zoom < 1 )
					return Mathf.map( _zoom, _minimumZoom, 1, -1, 0 );
				else
					return Mathf.map( _zoom, 1, _maximumZoom, 0, 1 );
			}
			set
			{
				var newZoom = Mathf.clamp( value, -1, 1 );
				if( newZoom == 0 )
					_zoom = 1f;
				else if( newZoom < 0 )
					_zoom = Mathf.map( newZoom, -1, 0, _minimumZoom, 1 );
				else
					_zoom = Mathf.map( newZoom, 0, 1, 1, _maximumZoom );
				
				_areMatrixesDirty = true;
			}
		}
		float _zoom;

		public float minimumZoom
		{
			get { return _minimumZoom; }
			set
			{
				Assert.isTrue( value > 0, "MinimumZoom must be greater than zero" );

				if( zoom < value )
					zoom = minimumZoom;

				_minimumZoom = value;
			}
		}
		float _minimumZoom = 0.1f;

		public float maximumZoom
		{
			get { return _maximumZoom; }
			set
			{
				Assert.isTrue( value > 0, "MaximumZoom must be greater than zero" );

				if( zoom > value )
					zoom = value;

				_maximumZoom = value;
			}
		}
		float _maximumZoom = 5f;

		/// <summary>
		/// world-space bounds of the camera. useful for culling.
		/// </summary>
		/// <value>The bounds.</value>
		public Rectangle bounds
		{
			get
			{
				if( _areMatrixesDirty )
					updateMatrixes();

				if( _areBoundsDirty )
				{
					// top-left and bottom-right are needed by either rotated or non-rotated bounds
					var topLeft = screenToWorldPoint( new Vector2( Core.graphicsDevice.Viewport.X, Core.graphicsDevice.Viewport.Y ) );
					var bottomRight = screenToWorldPoint( new Vector2( Core.graphicsDevice.Viewport.X + Core.graphicsDevice.Viewport.Width, Core.graphicsDevice.Viewport.Y + Core.graphicsDevice.Viewport.Height ) );

					if( rotation != 0 )
					{
						// special care for rotated bounds. we need to find our absolute min/max values and create the bounds from that
						var topRight = screenToWorldPoint( new Vector2( Core.graphicsDevice.Viewport.X + Core.graphicsDevice.Viewport.Width, Core.graphicsDevice.Viewport.Y ) );
						var bottomLeft = screenToWorldPoint( new Vector2( Core.graphicsDevice.Viewport.X, Core.graphicsDevice.Viewport.Y + Core.graphicsDevice.Viewport.Height ) );	

						var minX = (int)Mathf.minOf( topLeft.X, bottomRight.X, topRight.X, bottomLeft.X );
						var maxX = (int)Mathf.maxOf( topLeft.X, bottomRight.X, topRight.X, bottomLeft.X );
						var minY = (int)Mathf.minOf( topLeft.Y, bottomRight.Y, topRight.Y, bottomLeft.Y );
						var maxY = (int)Mathf.maxOf( topLeft.Y, bottomRight.Y, topRight.Y, bottomLeft.Y );

						_bounds.Location = new Point( minX, minY );
						_bounds.Width = (int)( maxX - minX );
						_bounds.Height = (int)( maxY - minY );
					}
					else
					{
						_bounds.Location = topLeft.ToPoint();
						_bounds.Width = (int)( bottomRight.X - topLeft.X );
						_bounds.Height = (int)( bottomRight.Y - topLeft.Y );
					}

					_areBoundsDirty = false;
				}

				return _bounds;
			}
		}
		Rectangle _bounds;

		public Matrix transformMatrix
		{
			get
			{
				if( _areMatrixesDirty )
					updateMatrixes();
				return _transformMatrix;
			}
		}
		Matrix _transformMatrix = Matrix.Identity;

		public Matrix inverseTransformMatrix
		{
			get
			{
				if( _areMatrixesDirty )
					updateMatrixes();
				return _inverseTransformMatrix;
			}
		}
		Matrix _inverseTransformMatrix = Matrix.Identity;


		float _near = -100f;
		float _far = 100f;
		bool _areMatrixesDirty = true;
		bool _areBoundsDirty = true;

		#endregion


		public Camera( float near = -100, float far = 100 )
		{
			rotation = 0;
			zoom = 0;
			_near = near;
			_far = far;

			// listen for screen resizes and graphics resets so we can dirty our bounds when it happens
			Core.emitter.addObserver( CoreEvents.GraphicsDeviceReset, onGraphicsDeviceReset );
		}


		void onGraphicsDeviceReset()
		{
			_areBoundsDirty = true;
		}


		void updateMatrixes()
		{
			if( !_areMatrixesDirty )
				return;
			
			Matrix tempMat;

			_transformMatrix = Matrix.CreateTranslation( -_position.X, -_position.Y, 0f ); // position
			Matrix.CreateScale( _zoom, _zoom, 1f, out tempMat ); // scale ->
			Matrix.Multiply( ref _transformMatrix, ref tempMat, out _transformMatrix );
			Matrix.CreateRotationZ( _rotation, out tempMat ); // rotation
			Matrix.Multiply( ref _transformMatrix, ref tempMat, out _transformMatrix );
			Matrix.CreateTranslation( (int)_origin.X, (int)_origin.Y, 0f, out tempMat ); // translate -origin
			Matrix.Multiply( ref _transformMatrix, ref tempMat, out _transformMatrix );

			// calculate our inverse as well
			Matrix.Invert( ref _transformMatrix, out _inverseTransformMatrix );

			// whenever the matrix changes the bounds are then invalid
			_areBoundsDirty = true;
			_areMatrixesDirty = false;
		}


		/// <summary>
		/// this forces the matrix and bounds dirty
		/// </summary>
		public void forceMatrixUpdate()
		{
			_areMatrixesDirty = _areBoundsDirty = true;
		}


		public void unload()
		{
			Core.emitter.removeObserver( CoreEvents.GraphicsDeviceReset, onGraphicsDeviceReset );
		}


		public void roundPosition()
		{
			Mathf.round( ref _position );
			_areMatrixesDirty = true;
		}


		public void centerOrigin()
		{
			origin = new Vector2( Core.graphicsDevice.Viewport.Width / 2f, Core.graphicsDevice.Viewport.Height / 2f );

			// offset our position to match the new center
			position += origin;
		}


		public void move( Vector2 direction )
		{
			position += Vector2.Transform( direction, Matrix.CreateRotationZ( -rotation ) );
		}


		public void rotate( float deltaRadians )
		{
			rotation += deltaRadians;
		}


		public void zoomIn( float deltaZoom )
		{
			zoom += deltaZoom;
		}


		public void zoomOut( float deltaZoom )
		{
			zoom -= deltaZoom;
		}


		/// <summary>
		/// converts a point from world coordinates to screen
		/// </summary>
		/// <returns>The to screen point.</returns>
		/// <param name="worldPosition">World position.</param>
		public Vector2 worldToScreenPoint( Vector2 worldPosition )
		{
			updateMatrixes();
			Vector2.Transform( ref worldPosition, ref _transformMatrix, out worldPosition );
			return worldPosition;
		}


		/// <summary>
		/// converts a point from screen coordinates to world
		/// </summary>
		/// <returns>The to world point.</returns>
		/// <param name="screenPosition">Screen position.</param>
		public Vector2 screenToWorldPoint( Vector2 screenPosition )
		{
			updateMatrixes();
			Vector2.Transform( ref screenPosition, ref _inverseTransformMatrix, out screenPosition );
			return screenPosition;
		}


		/// <summary>
		/// converts a oint from screen coordinates to world
		/// </summary>
		/// <returns>The to world point.</returns>
		/// <param name="screenPosition">Screen position.</param>
		public Vector2 screenToWorldPoint( Point screenPosition )
		{
			return screenToWorldPoint( screenPosition.ToVector2() );
		}


		/// <summary>
		/// gets this cameras project matrix
		/// </summary>
		/// <returns>The projection matrix.</returns>
		public Matrix getProjectionMatrix()
		{
			// not currently blocked with a dirty flag due to the core engine not using this
			return Matrix.CreateOrthographicOffCenter( 0, Core.graphicsDevice.Viewport.Width, Core.graphicsDevice.Viewport.Height, 0, _near, _far );
		}


		/// <summary>
		/// gets the view-projection matrix which is the transformMatrix * the projection matrix
		/// </summary>
		/// <returns>The view projection matrix.</returns>
		public Matrix getViewProjectionMatrix()
		{
			// not currently blocked with a dirty flag due to the core engine not using this
			return transformMatrix * getProjectionMatrix();
		}

	}
}


//------------------------------------------------------------------------------
//      Copyright (c) Microsoft Corporation.  All rights reserved.                                                             
//------------------------------------------------------------------------------

using System;
using System.Drawing;

namespace OrganismBase 
{
    /// <summary>
    ///  <para>
    ///   A classic algebraic vector class.  This class contains methods
    ///   that are integral to complex movement algorithms within the
    ///   Terrarium.
    ///  </para>
    /// </summary>
    public class Vector
    {
        /// <summary>
        ///  The value of the x component.
        /// </summary>
        double x;
        /// <summary>
        ///  The value of the y component.
        /// </summary>
        double y;
    
        /// <summary>
        ///  <para>
        ///   Constructs a new Vector using an x and y coordinate pair.
        ///  </para>
        /// </summary>
        /// <param name="x">
        ///  System.Double for the x coordinate for this vector.
        /// </param>
        /// <param name="y">
        ///  System.Double for the y coordinate for this vector.
        /// </param>
        public Vector(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    
        /// <summary>
        ///  <para>
        ///   Constructs a new Vector using System.Drawing.Point
        ///  </para>
        /// </summary>
        /// <param name="point">
        ///  System.Drawing.Point for the x,y coordinate pair for this vector.
        /// </param>
        public Vector(Point point)
        {
            this.x = point.X;
            this.y = point.Y;
        }

        /// <summary>
        ///  <para>
        ///   Used to retrieve the X component of the vector.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double for the X coordinate.
        /// </returns>
        public double X
        {
            get
            {
                return x;
            }
        }
    
        /// <summary>
        ///  <para>
        ///   Used to retrieve the Y component of the vector.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double for the Y coordinate.
        /// </returns>
        public double Y
        {
            get
            {
                return y;
            }
        }
    
        /// <summary>
        ///  <para>
        ///   Returns the x,y coordinate pair in the form of a System.Drawing.Point
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Drawing.Point containing the x,y coordinates
        /// </returns>
        public Point Point
        {
            get
            {
                return new Point((int) x, (int) y);
            }
        }

        /// <summary>
        ///  <para>
        ///   Used to compute the magnitude of the vector with respect
        ///   to the origin.
        ///  </para>
        ///  <para>
        ///   Returns a very fast approximate magnitude using a Taylor function
        ///   accurate to within 10%.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double representing the magnitude of the vector
        /// </returns>
        public double Magnitude
        {
            get
            {
                return FastMagnitude;
            }
        }

        /// <summary>
        ///  <para>
        ///   Used to compute the real magnitude of the vector with respect
        ///   to the origin.
        ///  </para>
        ///  <para>
        ///   Returns a very accurate magnitude result.  However, this function
        ///   tends to run much slower than an approximation.  For this reason
        ///   the Terrarium uses the Magnitude property internally.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double representing the magnitude of the vector
        /// </returns>
        public double TrueMagnitude
        {
            get
            {
                return Math.Sqrt((double) (X * X) + (Y * Y));
            }
        }

        /// <summary>
        ///  <para>
        ///   Returns the direction of the vector in Radians with 0 facing
        ///   East up to 2pi.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.Double representing the direction of the vector.
        /// </returns>
        public double Direction
        {
            get
            {
                double direction = Math.Atan2(y, x); 
                if (direction < 0)
                {
                    direction = 2 * Math.PI + direction;
                }

                return direction;
            }
        }

        /// <returns>
        /// Returns a very fast approximate magnitude using a Taylor function
        /// accurate to within 10%.
        /// Uses the equation: distance = abs(x1 - x2) + abs(y1-y2) - min(abs(x1-x2), abs(y1-y2)) /2
        /// since we're doing magnitude, x2 and y2 are both zero
        /// </returns>
        internal double FastMagnitude
        {
            get
            {
                double absX = (x < 0 ? -x : x);
                double absY = (y < 0 ? -y : y);
        
                if (absX < absY)
                {
                    return absX + absY - absX / 2;
                }
                else
                {
                    return absX + absY - absY / 2;
                }
            }
        }

        /// <summary>
        ///  <para>
        ///   Scales a vector by a constant value and returns a
        ///   new vector.  
        ///  </para>
        /// </summary>
        /// <param name="scalar">
        ///  System.Double representing the scalar value.
        /// </param>
        /// <returns>
        ///  Vector representing the new scaled vector
        /// </returns>
        public Vector Scale(double scalar)
        {
            return new Vector(x * scalar, y * scalar);
        }

        /// <summary>
        ///  <para>
        ///   Helper function used to convert degrees
        ///   to radians.
        ///  </para>
        /// </summary>
        /// <param name="degrees">
        ///  System.Double representing the amount in degrees.
        /// </param>
        /// <returns>
        ///  System.Double representing the amount in radians.
        /// </returns>
        static public double ToRadians(double degrees)
        {
            return (degrees / (double) 360) * 2 * Math.PI;
        }

        /// <summary>
        ///  <para>
        ///   Helper function used to convert radians
        ///   to degrees.
        ///  </para>
        /// </summary>
        /// <param name="radians">
        ///  System.Double representing the amount in radians.
        /// </param>
        /// <returns>
        ///  System.Double representing the amount in degrees.
        /// </returns>
        static public double ToDegrees(double radians)
        {
            return (radians / (2 * Math.PI)) * 360;
        }

        /// <summary>
        ///  <para>
        ///   Rotates a vector about the origin by an angle given in Radians
        ///   radians.
        ///  </para>
        ///  <para>
        ///   A point (x,y) can be rotated around the origin (0,0) by running it through the following equations 
        ///   to get the new point (x',y'):
        ///   x' = cos(theta)*x - sin(theta)*y 
        ///   y' = sin(theta)*x + cos(theta)*y
        ///   where theta is the angle by which to rotate the point.
        ///  </para>
        /// </summary>
        /// <param name="radians">
        ///  System.Double for the rotation angle in radians.
        /// </param>
        /// <returns>
        ///  Vector representing the newly rotated vector.
        /// </returns>
        public Vector Rotate(double radians)
        {
            Vector newVector = new Vector(  Math.Cos(radians) * x - Math.Sin(radians) * y, 
                Math.Sin(radians) * x + Math.Cos(radians) * y);
            return newVector;
        }
    
        /// <summary>
        ///  <para>
        ///   Used to get the unit vector for the current vector.  The unit
        ///   vector should have no component greater than 1.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  Vector representing the unit vector of the current vector.
        /// </returns>
        public Vector GetUnitVector()
        {
            double magnitude = Magnitude;
            
            return new Vector(x / magnitude, y / magnitude);
        }
    
        /// <summary>
        ///  <para>
        ///   Helper function that subtracts two points and computes
        ///   the resulting vector.
        ///  </para>
        /// </summary>
        /// <param name="point1">
        ///  System.Drawing.Point representing the first point
        /// </param>
        /// <param name="point2">
        ///  System.Drawing.Point representing the point to subtract from the first.
        /// </param>
        /// <returns>
        ///  Vector representing the result of the subtraction.
        /// </returns>
        static public Vector Subtract(Point point1, Point point2)
        {
            return new Vector(point2.X - point1.X, point2.Y - point1.Y);
        }
    
        /// <summary>
        ///  <para>
        ///   Helper function that adds the components values of
        ///   a point to an existing vector and returns the result
        ///   as a new Point.
        ///  </para>
        /// </summary>
        /// <param name="point">
        ///  System.Drawing.Point containing x,y components to add to vector.
        /// </param>
        /// <param name="vector">
        ///  Vector containing x,y components to be added to point
        /// </param>
        /// <returns>
        ///  System.Drawing.Point of the combined x,y components of vector and point.
        /// </returns>
        static public Point Add(Point point, Vector vector)
        {
            return new Point(point.X + (int) vector.X, point.Y + (int) vector.Y);
        }

        /// <summary>
        ///  <para>
        ///   Converts the given vector into a textual representation
        ///   useful for debugging purposes.
        ///  </para>
        /// </summary>
        /// <returns>
        ///  System.String describing the vector.
        /// </returns>
        public override string ToString()
        {
            return "{" + x.ToString() + ", " + y.ToString() + ", mag=" + Magnitude + "}";
        }    
    }
}
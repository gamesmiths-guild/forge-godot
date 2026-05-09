# Property Resolvers

Property resolvers provide **read-only computed values** that nodes can bind to as input properties. Each resolver returns a value from the current `GraphContext`, whether that value comes from graph data, entity state, activation data, math expressions, or random generation.

For how resolvers fit into Statescript's data flow, see [Variables and Data](variables.md). For creating your own resolvers, see [Custom Resolvers](custom-resolvers.md).

This page keeps the Godot documentation concise by listing the resolvers available in Forge for Godot and linking to the corresponding core Forge documentation for resolver behavior details where available.

> **Note:** `ActivationDataResolver` is specific to Forge for Godot's Statescript workflow, so it does not have a matching page in the core Forge resolver reference.

## Built-in Resolvers

| Resolver | Output Type | Description |
| --- | --- | --- |
| [ArrayVariableResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/array-resolver.md) | `(configured)` | Stores a mutable array of values with indexed access. |
| ActivationDataResolver | `(configured)` | Reads a field from the graph's configured activation data provider. |
| [AttributeResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/attribute-resolver.md) | `int` | Reads the current value of an entity attribute. |
| [MagnitudeResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/magnitude-resolver.md) | `float` | Reads the magnitude from the ability activation context. |
| [SharedVariableResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/shared-variable-resolver.md) | `(configured)` | Reads a shared variable from the entity. |
| [TagResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/tag-resolver.md) | `bool` | Checks whether the owner entity has a specific tag. |
| [VariableResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/variable-resolver.md) | `(configured)` | Reads a graph variable by name. |
| [VariantResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/variant-resolver.md) | `(configured)` | Holds a fixed constant value. |

## Boolean Expressions

| Resolver | Output Type | Description |
| --- | --- | --- |
| [AndResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/and-resolver.md) | `bool` | Returns `true` only when both boolean operands are `true`. |
| [ComparisonResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/comparison-resolver.md) | `bool` | Compares two values using a comparison operation. |
| [NotResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/not-resolver.md) | `bool` | Returns the logical inverse of a boolean operand. |
| [OrResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/or-resolver.md) | `bool` | Returns `true` when either boolean operand is `true`. |
| [XorResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/xor-resolver.md) | `bool` | Returns `true` when exactly one boolean operand is `true`. |

## Math

### Scalar Math Resolvers

| Resolver | Output Type | Description |
| --- | --- | --- |
| [ACosHResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/acosh-resolver.md) | `float`/`double` | Computes the inverse hyperbolic cosine. |
| [ACosResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/acos-resolver.md) | `float`/`double` | Computes the arc cosine (inverse cosine), returning angle in radians. |
| [ASinHResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/asinh-resolver.md) | `float`/`double` | Computes the inverse hyperbolic sine. |
| [ASinResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/asin-resolver.md) | `float`/`double` | Computes the arc sine (inverse sine), returning angle in radians. |
| [ATan2Resolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/atan2-resolver.md) | `float`/`double` | Computes the angle from two coordinates using `ATan2(y, x)`. |
| [ATanHResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/atanh-resolver.md) | `float`/`double` | Computes the inverse hyperbolic tangent. |
| [ATanResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/atan-resolver.md) | `float`/`double` | Computes the arc tangent (inverse tangent), returning angle in radians. |
| [CbrtResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/cbrt-resolver.md) | `float`/`double` | Computes the cube root. |
| [CopySignResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/copysign-resolver.md) | `float`/`double` | Returns a value with the magnitude of one operand and the sign of another. |
| [CosHResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/cosh-resolver.md) | `float`/`double` | Computes the hyperbolic cosine. |
| [CosResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/cos-resolver.md) | `float`/`double` | Computes the cosine of an angle in radians. |
| [EResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/e-resolver.md) | `float`/`double` | Returns the mathematical constant `e` (Euler's number). |
| [ExpResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/exp-resolver.md) | `float`/`double` | Computes `e` raised to a specified power (`e^x`). |
| [Log10Resolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/log10-resolver.md) | `float`/`double` | Computes the base-10 logarithm. |
| [Log2Resolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/log2-resolver.md) | `float`/`double` | Computes the base-2 logarithm. |
| [LogResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/log-resolver.md) | `float`/`double` | Computes the natural logarithm (base `e`). |
| [PiResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/pi-resolver.md) | `float`/`double` | Returns the mathematical constant π (pi). |
| [SignResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/sign-resolver.md) | `int` | Returns -1, 0, or 1 indicating the sign of a numeric value. |
| [SinHResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/sinh-resolver.md) | `float`/`double` | Computes the hyperbolic sine. |
| [SinResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/sin-resolver.md) | `float`/`double` | Computes the sine of an angle in radians. |
| [TanHResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/tanh-resolver.md) | `float`/`double` | Computes the hyperbolic tangent. |
| [TanResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/tan-resolver.md) | `float`/`double` | Computes the tangent of an angle in radians. |

### Generic Math Resolvers

| Resolver | Output Type | Description |
| --- | --- | --- |
| [AbsResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/abs-resolver.md) | `(promoted or same vector type)` | Computes the absolute value of a signed numeric value or vector components. |
| [AddResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/add-resolver.md) | `(promoted or same vector type)` | Adds two numeric, vector or quaternion values. |
| [CeilResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ceil-resolver.md) | `(same)` | Rounds up to the smallest integer greater than or equal to the operand. |
| [ClampResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/clamp-resolver.md) | `(promoted or same vector type)` | Clamps a numeric value or vector components between minimum and maximum bounds. |
| [DegToRadResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/degtorad-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Converts degrees to radians. |
| [DivideResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/divide-resolver.md) | `(promoted or same vector type)` | Divides two numeric values, vectors component-wise, or two quaternions. |
| [FloorResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/floor-resolver.md) | `(same)` | Rounds down to the largest integer less than or equal to the operand. |
| [LerpResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/lerp-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4`/`Quaternion` | Linearly interpolates between two values (scalar, vector, or quaternion). |
| [MaxResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/max-resolver.md) | `(promoted or same vector type)` | Returns the larger of two numeric values or the component-wise maximum of two vectors. |
| [MinResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/min-resolver.md) | `(promoted or same vector type)` | Returns the smaller of two numeric values or the component-wise minimum of two vectors. |
| [ModuloResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/modulo-resolver.md) | `(promoted)` | Computes the remainder of dividing two numeric values. |
| [MultiplyResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/multiply-resolver.md) | `(promoted or same vector type)` | Multiplies two numeric, vectors component-wise, or two quaternions. |
| [NegateResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/negate-resolver.md) | `(promoted)` | Negates a numeric or vector value. |
| [PowResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/pow-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Raises a value to a specified power. |
| [RadToDegResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/radtodeg-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Converts radians to degrees. |
| [RoundResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/round-resolver.md) | `(same)` | Rounds to a specified number of digits with configurable rounding mode. |
| [SqrtResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/sqrt-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Computes the square root of a numeric value or component-wise square root of a vector. |
| [SubtractResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/subtract-resolver.md) | `(promoted or same vector type)` | Subtracts two numeric, vector or quaternion values. |
| [TruncateResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/truncate-resolver.md) | `(same)` | Removes the fractional part, rounding toward zero. |

## Spatial Math

### Vector

| Resolver | Output Type | Description |
| --- | --- | --- |
| [AngleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/angle-resolver.md) | `float` | Computes the unsigned angle between two vectors or two quaternions. |
| [ClampMagnitudeResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/clampmagnitude-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Clamps a vector to a maximum magnitude. |
| [CrossResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/cross-resolver.md) | `Vector3` | Computes the cross product of two `Vector3` operands. |
| [DistanceResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/distance-resolver.md) | `float` | Computes the Euclidean distance between two vector operands. |
| [DistanceSquaredResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/distancesquared-resolver.md) | `float` | Computes the squared Euclidean distance between two vector operands. |
| [DotResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/dot-resolver.md) | `float` | Computes the dot product of two vectors or two quaternions. |
| [LengthResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/length-resolver.md) | `float` | Computes the length (magnitude) of a vector or quaternion operand. |
| [LengthSquaredResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/lengthsquared-resolver.md) | `float` | Computes the squared length of a vector or quaternion operand. |
| [NormalizeResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/normalize-resolver.md) | `Vector2`/`Vector3`/`Vector4`/`Plane`/`Quaternion` | Computes the normalized form of a vector, plane, or quaternion. |
| [ProjectResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/project-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Projects one vector onto another. |
| [ReflectResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/reflect-resolver.md) | `Vector2`/`Vector3` | Reflects a vector off a surface defined by a normal vector. |
| [RejectResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/reject-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Rejects one vector from another. |
| [ScaleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/scale-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Scales a vector by a float scalar value. |
| [SignedAngleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/signedangle-resolver.md) | `float` | Computes the signed angle between two vectors. |
| [VectorComponentResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/vectorcomponent-resolver.md) | `float` | Extracts a single component from a vector. |
| [VectorFromValuesResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/vectorfromvalues-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Creates a vector from float component resolver values. |

### Quaternion

| Resolver | Output Type | Description |
| --- | --- | --- |
| [ConcatenateResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/concatenate-resolver.md) | `Quaternion` | Concatenates two quaternion rotations. |
| [ConjugateResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/conjugate-resolver.md) | `Quaternion` | Computes the conjugate of a quaternion. |
| [InverseResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/inverse-resolver.md) | `Quaternion` | Computes the inverse of a quaternion. |
| [LookAtResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/lookat-resolver.md) | `Quaternion` | Creates a look rotation from one position to another using an up vector. |
| [QuaternionFromAxisAngleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/quaternionfromaxisangle-resolver.md) | `Quaternion` | Creates a quaternion from an axis and angle. |
| [QuaternionFromEulerAnglesResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/quaternionfromeulerangles-resolver.md) | `Quaternion` | Creates a quaternion from Euler angles using an optional Euler order. |
| [QuaternionFromYawPitchRollResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/quaternionfromyawpitchroll-resolver.md) | `Quaternion` | Creates a quaternion from yaw, pitch, and roll angles. |
| [RotateTowardsResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/rotatetowards-resolver.md) | `Quaternion` | Rotates one quaternion toward another by a maximum angular delta. |
| [SlerpResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/slerp-resolver.md) | `Quaternion` | Spherically interpolates between two quaternion rotations. |

### Plane

| Resolver | Output Type | Description |
| --- | --- | --- |
| [DotCoordinateResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/dotcoordinate-resolver.md) | `float` | Computes the dot product of a plane and a 3D coordinate. |
| [DotNormalResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/dotnormal-resolver.md) | `float` | Computes the dot product of a plane normal and a vector. |
| [PlaneDistanceResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/planedistance-resolver.md) | `float` | Extracts the distance component of a plane. |
| [PlaneFromNormalResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/planefromnormal-resolver.md) | `Plane` | Creates a plane from a normal vector and distance. |
| [PlaneFromVerticesResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/planefromvertices-resolver.md) | `Plane` | Creates a plane from three vertices. |
| [PlaneNormalResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/planenormal-resolver.md) | `Vector3` | Extracts the normal component of a plane. |

### Utility

| Resolver | Output Type | Description |
| --- | --- | --- |
| [EulerAnglesFromQuaternionResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/euleranglesfromquaternion-resolver.md) | `Vector3` | Extracts Euler angles from a quaternion using an optional Euler order. |
| [MoveTowardsResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/movetowards-resolver.md) | `float`/`Vector2`/`Vector3`/`Vector4` | Moves a value toward a target by a maximum delta. |
| [TransformResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/transform-resolver.md) | `Vector2`/`Vector3`/`Vector4`/`Plane` | Transforms a vector or plane by a quaternion rotation. |

## Random

| Resolver | Output Type | Description |
| --- | --- | --- |
| [RandomDirectionResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/randomdirection-resolver.md) | `Vector2` | Returns a random normalized 2D direction. |
| [RandomInsideCircleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/randominsidecircle-resolver.md) | `Vector2` | Returns a random point inside the unit circle. |
| [RandomInsideSphereResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/randominsidesphere-resolver.md) | `Vector3` | Returns a random point inside the unit sphere. |
| [RandomOnSphereResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/randomonsphere-resolver.md) | `Vector3` | Returns a random normalized 3D direction on the unit sphere. |
| [RandomResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/random-resolver.md) | `int`/`float`/`double` | Generates a random value in a range using an `IRandom` provider. |

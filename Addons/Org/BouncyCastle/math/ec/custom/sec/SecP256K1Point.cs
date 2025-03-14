using System;

using MultiServer.Addons.Org.BouncyCastle.Math.Raw;

namespace MultiServer.Addons.Org.BouncyCastle.Math.EC.Custom.Sec
{
    internal class SecP256K1Point
        : AbstractFpPoint
    {
        internal SecP256K1Point(ECCurve curve, ECFieldElement x, ECFieldElement y)
            : base(curve, x, y)
        {
        }

        internal SecP256K1Point(ECCurve curve, ECFieldElement x, ECFieldElement y, ECFieldElement[] zs)
            : base(curve, x, y, zs)
        {
        }

        protected override ECPoint Detach()
        {
            return new SecP256K1Point(null, AffineXCoord, AffineYCoord);
        }

        public override ECPoint Add(ECPoint b)
        {
            if (this.IsInfinity)
                return b;
            if (b.IsInfinity)
                return this;
            if (this == b)
                return Twice();

            ECCurve curve = this.Curve;

            SecP256K1FieldElement X1 = (SecP256K1FieldElement)this.RawXCoord, Y1 = (SecP256K1FieldElement)this.RawYCoord;
            SecP256K1FieldElement X2 = (SecP256K1FieldElement)b.RawXCoord, Y2 = (SecP256K1FieldElement)b.RawYCoord;

            SecP256K1FieldElement Z1 = (SecP256K1FieldElement)this.RawZCoords[0];
            SecP256K1FieldElement Z2 = (SecP256K1FieldElement)b.RawZCoords[0];

            uint c;
            uint[] tt0 = Nat256.CreateExt();
            uint[] tt1 = Nat256.CreateExt();
            uint[] t2 = Nat256.Create();
            uint[] t3 = Nat256.Create();
            uint[] t4 = Nat256.Create();

            bool Z1IsOne = Z1.IsOne;
            uint[] U2, S2;
            if (Z1IsOne)
            {
                U2 = X2.x;
                S2 = Y2.x;
            }
            else
            {
                S2 = t3;
                SecP256K1Field.Square(Z1.x, S2, tt0);

                U2 = t2;
                SecP256K1Field.Multiply(S2, X2.x, U2, tt0);

                SecP256K1Field.Multiply(S2, Z1.x, S2, tt0);
                SecP256K1Field.Multiply(S2, Y2.x, S2, tt0);
            }

            bool Z2IsOne = Z2.IsOne;
            uint[] U1, S1;
            if (Z2IsOne)
            {
                U1 = X1.x;
                S1 = Y1.x;
            }
            else
            {
                S1 = t4;
                SecP256K1Field.Square(Z2.x, S1, tt0);

                U1 = tt1;
                SecP256K1Field.Multiply(S1, X1.x, U1, tt0);

                SecP256K1Field.Multiply(S1, Z2.x, S1, tt0);
                SecP256K1Field.Multiply(S1, Y1.x, S1, tt0);
            }

            uint[] H = Nat256.Create();
            SecP256K1Field.Subtract(U1, U2, H);

            uint[] R = t2;
            SecP256K1Field.Subtract(S1, S2, R);

            // Check if b == this or b == -this
            if (Nat256.IsZero(H))
            {
                if (Nat256.IsZero(R))
                {
                    // this == b, i.e. this must be doubled
                    return this.Twice();
                }

                // this == -b, i.e. the result is the point at infinity
                return curve.Infinity;
            }

            uint[] HSquared = t3;
            SecP256K1Field.Square(H, HSquared, tt0);

            uint[] G = Nat256.Create();
            SecP256K1Field.Multiply(HSquared, H, G, tt0);

            uint[] V = t3;
            SecP256K1Field.Multiply(HSquared, U1, V, tt0);

            SecP256K1Field.Negate(G, G);
            Nat256.Mul(S1, G, tt1);

            c = Nat256.AddBothTo(V, V, G);
            SecP256K1Field.Reduce32(c, G);

            SecP256K1FieldElement X3 = new SecP256K1FieldElement(t4);
            SecP256K1Field.Square(R, X3.x, tt0);
            SecP256K1Field.Subtract(X3.x, G, X3.x);

            SecP256K1FieldElement Y3 = new SecP256K1FieldElement(G);
            SecP256K1Field.Subtract(V, X3.x, Y3.x);
            SecP256K1Field.MultiplyAddToExt(Y3.x, R, tt1);
            SecP256K1Field.Reduce(tt1, Y3.x);

            SecP256K1FieldElement Z3 = new SecP256K1FieldElement(H);
            if (!Z1IsOne)
            {
                SecP256K1Field.Multiply(Z3.x, Z1.x, Z3.x, tt0);
            }
            if (!Z2IsOne)
            {
                SecP256K1Field.Multiply(Z3.x, Z2.x, Z3.x, tt0);
            }

            ECFieldElement[] zs = new ECFieldElement[] { Z3 };

            return new SecP256K1Point(curve, X3, Y3, zs);
        }

        public override ECPoint Twice()
        {
            if (this.IsInfinity)
                return this;

            ECCurve curve = this.Curve;

            SecP256K1FieldElement Y1 = (SecP256K1FieldElement)this.RawYCoord;
            if (Y1.IsZero)
                return curve.Infinity;

            SecP256K1FieldElement X1 = (SecP256K1FieldElement)this.RawXCoord, Z1 = (SecP256K1FieldElement)this.RawZCoords[0];

            uint c;
            uint[] tt0 = Nat256.CreateExt();

            uint[] Y1Squared = Nat256.Create();
            SecP256K1Field.Square(Y1.x, Y1Squared, tt0);

            uint[] T = Nat256.Create();
            SecP256K1Field.Square(Y1Squared, T, tt0);

            uint[] M = Nat256.Create();
            SecP256K1Field.Square(X1.x, M, tt0);
            c = Nat256.AddBothTo(M, M, M);
            SecP256K1Field.Reduce32(c, M);

            uint[] S = Y1Squared;
            SecP256K1Field.Multiply(Y1Squared, X1.x, S, tt0);
            c = Nat.ShiftUpBits(8, S, 2, 0);
            SecP256K1Field.Reduce32(c, S);

            uint[] t1 = Nat256.Create();
            c = Nat.ShiftUpBits(8, T, 3, 0, t1);
            SecP256K1Field.Reduce32(c, t1);

            SecP256K1FieldElement X3 = new SecP256K1FieldElement(T);
            SecP256K1Field.Square(M, X3.x, tt0);
            SecP256K1Field.Subtract(X3.x, S, X3.x);
            SecP256K1Field.Subtract(X3.x, S, X3.x);

            SecP256K1FieldElement Y3 = new SecP256K1FieldElement(S);
            SecP256K1Field.Subtract(S, X3.x, Y3.x);
            SecP256K1Field.Multiply(Y3.x, M, Y3.x, tt0);
            SecP256K1Field.Subtract(Y3.x, t1, Y3.x);

            SecP256K1FieldElement Z3 = new SecP256K1FieldElement(M);
            SecP256K1Field.Twice(Y1.x, Z3.x);
            if (!Z1.IsOne)
            {
                SecP256K1Field.Multiply(Z3.x, Z1.x, Z3.x, tt0);
            }

            return new SecP256K1Point(curve, X3, Y3, new ECFieldElement[] { Z3 });
        }

        public override ECPoint TwicePlus(ECPoint b)
        {
            if (this == b)
                return ThreeTimes();
            if (this.IsInfinity)
                return b;
            if (b.IsInfinity)
                return Twice();

            ECFieldElement Y1 = this.RawYCoord;
            if (Y1.IsZero)
                return b;

            return Twice().Add(b);
        }

        public override ECPoint ThreeTimes()
        {
            if (this.IsInfinity || this.RawYCoord.IsZero)
                return this;

            // NOTE: Be careful about recursions between TwicePlus and ThreeTimes
            return Twice().Add(this);
        }

        public override ECPoint Negate()
        {
            if (IsInfinity)
                return this;

            return new SecP256K1Point(Curve, RawXCoord, RawYCoord.Negate(), RawZCoords);
        }
    }
}

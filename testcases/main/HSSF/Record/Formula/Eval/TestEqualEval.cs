/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace TestCases.HSSF.Record.Formula.Eval
{

    using NPOI.HSSF.Record.Formula.Eval;
    using TestCases.HSSF.Record.Formula.Functions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using HSSFFunctions = NPOI.HSSF.Record.Formula.Functions;
    using System;

    /**
     * Test for {@link EqualEval}
     *
     * @author Josh Micich
     */
    [TestClass]
    public class TestEqualEval 
    {
        /// <summary>
        ///  Some of the tests are depending on the american culture.
        /// </summary>
        [ClassInitialize()]
        public static void PrepareCultere(TestContext testContext)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
        }

        /**
         * Test for bug observable at svn revision 692218 (Sep 2008)<br/>
         * The value from a 1x1 area should be taken immediately, regardless of srcRow and srcCol
         */
        [TestMethod]
        public void Test1x1AreaOperand()
        {

            ValueEval[] values = { BoolEval.FALSE, };
            ValueEval[] args = {
			EvalFactory.CreateAreaEval("B1:B1", values),
			BoolEval.FALSE,
		};
            ValueEval result = Evaluate(EvalInstances.Equal, args, 10, 10);
            if (result is ErrorEval)
            {
                if (result == ErrorEval.VALUE_INVALID)
                {
                    throw new AssertFailedException("Identified bug in Evaluation of 1x1 area");
                }
            }
            Assert.AreEqual(typeof(BoolEval), result.GetType());
            Assert.IsTrue(((BoolEval)result).BooleanValue);
        }
        /**
         * Empty string is equal to blank
         */
        [TestMethod]
        public void TestBlankEqualToEmptyString()
        {

            ValueEval[] args = {
			new StringEval(""),
			BlankEval.instance,
		};
            ValueEval result = Evaluate(EvalInstances.Equal, args, 10, 10);
            Assert.AreEqual(typeof(BoolEval), result.GetType());
            BoolEval be = (BoolEval)result;
            if (!be.BooleanValue)
            {
                throw new AssertFailedException("Identified bug blank/empty string Equality");
            }
            Assert.IsTrue(be.BooleanValue);
        }

        /**
         * Test for bug 46613 (observable at svn r737248)
         */
        [TestMethod]
        public void TestStringInsensitive_bug46613()
        {
            if (!EvalStringCmp("abc", "aBc", EvalInstances.Equal))
            {
                throw new AssertFailedException("Identified bug 46613");
            }
            Assert.IsTrue(EvalStringCmp("abc", "aBc", EvalInstances.Equal));
            Assert.IsTrue(EvalStringCmp("ABC", "azz", EvalInstances.LessThan));
            Assert.IsTrue(EvalStringCmp("abc", "AZZ", EvalInstances.LessThan));
            Assert.IsTrue(EvalStringCmp("ABC", "aaa", EvalInstances.GreaterThan));
            Assert.IsTrue(EvalStringCmp("abc", "AAA", EvalInstances.GreaterThan));
        }

        private static bool EvalStringCmp(String a, String b, HSSFFunctions.Function cmpOp)
        {
            ValueEval[] args = {
			new StringEval(a),
			new StringEval(b),
		};
            ValueEval result = Evaluate(cmpOp, args, 10, 20);
            Assert.AreEqual(typeof(BoolEval), result.GetType());
            BoolEval be = (BoolEval)result;
            return be.BooleanValue;
        }

        /**
         * Bug 47198 involved a formula "-A1=0" where cell A1 was 0.0.
         * Excel Evaluates "-A1=0" to TRUE, not because it thinks -0.0==0.0
         * but because "-A1" Evaluated to +0.0
         * <p/>
         * Note - the original diagnosis of bug 47198 was that
         * "Excel considers -0.0 to be equal to 0.0" which is NQR
         * See {@link TestMinusZeroResult} for more specific tests regarding -0.0.
         */
        [TestMethod]
        public void TestZeroEquality_bug47198()
        {
            NumberEval zero = new NumberEval(0.0);
            NumberEval mZero = (NumberEval)Evaluate(UnaryMinusEval.instance, new ValueEval[] { zero, }, 0, 0);
            if (BitConverter.DoubleToInt64Bits(mZero.NumberValue) == unchecked((long)0x8000000000000000L))
            {
                throw new AssertFailedException("Identified bug 47198: unary minus should convert -0.0 to 0.0");
            }
            ValueEval[] args = { zero, mZero, };
            BoolEval result = (BoolEval)Evaluate(EvalInstances.Equal, args, 0, 0);
            if (!result.BooleanValue)
            {
                throw new AssertFailedException("Identified bug 47198: -0.0 != 0.0");
            }
        }
        [TestMethod]
        [Ignore] // TODO port upstream patch https://svn.apache.org/viewvc?view=revision&revision=798771
        public void TestRounding_bug47598()
        {
            double x = 1 + 1.0028 - 0.9973; // should be 1.0055, but has IEEE rounding
            Assert.IsFalse(x == 1.0055);

            NumberEval a = new NumberEval(x);
            NumberEval b = new NumberEval(1.0055);
            Assert.AreEqual("1.0055", b.StringValue);

            ValueEval[] args = { a, b, };
            BoolEval result = (BoolEval)Evaluate(EvalInstances.Equal, args, 0, 0);
            if (!result.BooleanValue)
            {
                throw new AssertFailedException("Identified bug 47598: 1+1.0028-0.9973 != 1.0055");
            }
        }

        private static ValueEval Evaluate(HSSFFunctions.Function oper, ValueEval[] args, int srcRowIx, int srcColIx)
        {
            return oper.Evaluate(args, srcRowIx, (short)srcColIx);
        }
    }
}




﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	public class RegCollItem
	{
		public int Value;

		public RegCollItem(int v)
		{
			Value = v;
		}
	}

	public class RegCollMethods
	{
		List<RegCollItem> m_Items = new List<RegCollItem>() { new RegCollItem(7), new RegCollItem(8), new RegCollItem(9) };
		List<int> m_List = new List<int>() { 1, 2, 3 };
		int[] m_Array = new int[3] { 2, 4, 6 };
		int[,] m_MultiArray = new int[2, 3] { { 2, 4, 6 }, { 7, 8, 9 } };

		public int[,] GetMultiArray()
		{
			return m_MultiArray;
		}

		public int[] GetArray()
		{
			return m_Array;
		}

		public List<RegCollItem> GetItems()
		{
			return m_Items;
		}

		public List<int> GetList()
		{
			return m_List;
		}

		public IEnumerator<int> GetEnumerator()
		{
			return GetList().GetEnumerator();
		}
	}



	[TestFixture]
	public class CollectionsBaseInterfGenRegisteredTests
	{
		void Do(string code, Action<DynValue> asserts)
		{
			Do(code, (d, o) => asserts(d));
		}


		void Do(string code, Action<DynValue, RegCollMethods> asserts)
		{
			try
			{
				UserData.RegisterType<RegCollMethods>();
				UserData.RegisterType<RegCollItem>();
				UserData.RegisterType(typeof(IList<>));

				Script s = new Script();

				var obj = new RegCollMethods();
				s.Globals["o"] = obj;
				s.Globals["ctor"] = UserData.CreateStatic<RegCollItem>();

				DynValue res = s.DoString(code);

				asserts(res, obj);
			}
			catch (ScriptRuntimeException ex)
			{
				Debug.WriteLine(ex.DecoratedMessage);
				ex.Rethrow();
				throw;
			}
			finally
			{
				UserData.UnregisterType<RegCollMethods>();
				UserData.UnregisterType<RegCollItem>();
				UserData.UnregisterType<Array>();
				UserData.UnregisterType(typeof(IList<>));
				UserData.UnregisterType(typeof(IList<RegCollItem>));
				UserData.UnregisterType(typeof(IList<int>));
				//UserData.UnregisterType<IEnumerable>();
			}
		}





		[Test]
		public void RegCollGenInterf_IteratorOnList_Auto()
		{
			Do(@"
				local list = o:GetList()

				local x = 0;
				for i in list do 
					x = x + i;
				end
				return x;
			",
			 (r) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(6, r.Number);
			 });
		}


		[Test]
		public void RegCollGenInterf_IteratorOnList_Manual()
		{
			Do(@"
				function each(obj)
					local e = obj:GetEnumerator()
					return function()
						if e:MoveNext() then
							return e.Current
						end
					end
				end

				local list = o; 

				local x = 0;
				for i in each(list) do 
					x = x + i;
				end
				return x;

			",
			 (r) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(6, r.Number);
			 });
		}

		[Test]
		public void RegCollGenInterf_IteratorOnList_ChangeElem()
		{
			Do(@"
				local list = o:GetList()

				list[1] = list[2] + list[1];

				local x = 0;
				for i in list do 
					x = x + i;
				end
				return x;
			",
			 (r, o) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(9, r.Number);
				 Assert.AreEqual(1, o.GetList()[0]);
				 Assert.AreEqual(5, o.GetList()[1]);
				 Assert.AreEqual(3, o.GetList()[2]);
			 });
		}


		[Test]
		public void RegCollGenInterf_IteratorOnArray_Auto()
		{
			Do(@"
				local array = o:GetArray()

				local x = 0;
				for i in array do 
					x = x + i;
				end
				return x;			",
			 (r) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(12, r.Number);
			 });
		}


		[Test]
		public void RegCollGenInterf_IteratorOnArray_ChangeElem()
		{
			Do(@"
				local array = o:GetArray()

				array[1] = array[2] - 1;

				local x = 0;
				for i in array do 
					x = x + i;
				end
				return x;
			",
			 (r, o) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(13, r.Number);
				 Assert.AreEqual(2, o.GetArray()[0]);
				 Assert.AreEqual(5, o.GetArray()[1]);
				 Assert.AreEqual(6, o.GetArray()[2]);
			 });
		}



		[Test]
		public void RegCollGenInterf_IteratorOnObjList_Auto()
		{
			Do(@"
				local list = o:GetItems()

				local x = 0;
				for i in list do 
					x = x + i.Value;
				end
				return x;
			",
			 (r) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(24, r.Number);
			 });
		}


		[Test]
		public void RegCollGenInterf_IteratorOnObjList_Manual()
		{
			Do(@"
				function each(obj)
					local e = obj:GetEnumerator()
					return function()
						if e:MoveNext() then
							return e.Current
						end
					end
				end

				local list = o.GetItems(); 

				local x = 0;
				for i in each(list) do 
					x = x + i.Value;
				end
				return x;

			",
			 (r) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(24, r.Number);
			 });
		}

		[Test]
		public void RegCollGenInterf_IteratorOnObjList_ChangeElem()
		{
			Do(@"
				local list = o:GetItems()

				list[1] = ctor.__new(list[2].Value + list[1].Value);

				local x = 0;
				for i in list do 
					x = x + i.Value;
				end
				return x;
			",
			 (r, o) =>
			 {
				 Assert.AreEqual(DataType.Number, r.Type);
				 Assert.AreEqual(7 + 17 + 9, r.Number);
				 Assert.AreEqual(7, o.GetItems()[0].Value);
				 Assert.AreEqual(17, o.GetItems()[1].Value);
				 Assert.AreEqual(9, o.GetItems()[2].Value);
			 });
		}



	}
}

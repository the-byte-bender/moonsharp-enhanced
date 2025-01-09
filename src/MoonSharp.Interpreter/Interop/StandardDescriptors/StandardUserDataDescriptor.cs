using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoonSharp.Interpreter.Compatibility;
using MoonSharp.Interpreter.Interop.BasicDescriptors;

namespace MoonSharp.Interpreter.Interop
{
	/// <summary>
	/// Standard descriptor for userdata types.
	/// </summary>
	public class StandardUserDataDescriptor : DispatchingUserDataDescriptor, IWireableDescriptor
	{
		/// <summary>
		/// Gets the interop access mode this descriptor uses for members access
		/// </summary>
		public InteropAccessMode AccessMode { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardUserDataDescriptor"/> class.
		/// </summary>
		/// <param name="type">The type this descriptor refers to.</param>
		/// <param name="accessMode">The interop access mode this descriptor uses for members access</param>
		/// <param name="friendlyName">A human readable friendly name of the descriptor.</param>
		public StandardUserDataDescriptor(Type type, InteropAccessMode accessMode, string friendlyName = null)
			: base(type, friendlyName)
		{
			if (accessMode == InteropAccessMode.NoReflectionAllowed)
				throw new ArgumentException("Can't create a StandardUserDataDescriptor under a NoReflectionAllowed access mode");

			if (Script.GlobalOptions.Platform.IsRunningOnAOT())
				accessMode = InteropAccessMode.Reflection;

			if (accessMode == InteropAccessMode.Default)
				accessMode = UserData.DefaultAccessMode;

			AccessMode = accessMode;

			FillMemberList();
		}

		private bool ShouldIncludeMember(string memberName, MemberInfo member, HashSet<string> membersToIgnore)
		{
			if (AccessMode == InteropAccessMode.HideMembers)
				return false;

			// First check the HideMember HashSet
			if (membersToIgnore.Contains(memberName))
				return false;

			// Get default visibility setting from the type
			var defaultVisibilityAttr = Framework.Do.GetCustomAttributes(Type, typeof(MoonSharpDefaultVisibilityAttribute), true)
				.OfType<MoonSharpDefaultVisibilityAttribute>()
				.FirstOrDefault();

			// If no default visibility attribute is present, maintain original behavior (visible by default)
			if (defaultVisibilityAttr == null)
				return true;

			// If default is visible, include unless hidden (which we checked above)
			if (defaultVisibilityAttr.IsVisibleByDefault)
				return true;

			// If default is hidden, only include if explicitly marked visible
			return member.GetVisibilityFromAttributes() == true;
		}

		/// <summary>
		/// Fills the member list.
		/// </summary>
		private void FillMemberList()
		{
			HashSet<string> membersToIgnore = new HashSet<string>(
				Framework.Do.GetCustomAttributes(this.Type, typeof(MoonSharpHideMemberAttribute), true)
					.OfType<MoonSharpHideMemberAttribute>()
					.Select(a => a.MemberName)
				);

			Type type = this.Type;

			if (AccessMode == InteropAccessMode.HideMembers)
				return;

			if (!type.IsDelegateType())
			{
				// Add constructors
				foreach (ConstructorInfo ci in Framework.Do.GetConstructors(type))
				{
					if (ShouldIncludeMember("__new", ci, membersToIgnore))
					{
						AddMember("__new", MethodMemberDescriptor.TryCreateIfVisible(ci, this.AccessMode));
					}
				}

				// Handle value type constructor
				if (Framework.Do.IsValueType(type) && !membersToIgnore.Contains("__new"))
				{
					AddMember("__new", new ValueTypeDefaultCtorMemberDescriptor(type));
				}
			}

			// Add methods
			foreach (MethodInfo mi in Framework.Do.GetMethods(type))
			{
				if (!ShouldIncludeMember(mi.Name, mi, membersToIgnore))
					continue;

				MethodMemberDescriptor md = MethodMemberDescriptor.TryCreateIfVisible(mi, this.AccessMode);

				if (md != null && MethodMemberDescriptor.CheckMethodIsCompatible(mi, false))
				{
					string name = mi.Name;
					if (mi.IsSpecialName && (mi.Name == SPECIALNAME_CAST_EXPLICIT || mi.Name == SPECIALNAME_CAST_IMPLICIT))
					{
						name = mi.ReturnType.GetConversionMethodName();
					}

					AddMember(name, md);

					foreach (string metaname in mi.GetMetaNamesFromAttributes())
					{
						AddMetaMember(metaname, md);
					}
				}
			}

			// Add properties
			foreach (PropertyInfo pi in Framework.Do.GetProperties(type))
			{
				if (!pi.IsSpecialName && !pi.GetIndexParameters().Any() && ShouldIncludeMember(pi.Name, pi, membersToIgnore))
				{
					AddMember(pi.Name, PropertyMemberDescriptor.TryCreateIfVisible(pi, this.AccessMode));
				}
			}

			// Add fields
			foreach (FieldInfo fi in Framework.Do.GetFields(type))
			{
				if (!fi.IsSpecialName && ShouldIncludeMember(fi.Name, fi, membersToIgnore))
				{
					AddMember(fi.Name, FieldMemberDescriptor.TryCreateIfVisible(fi, this.AccessMode));
				}
			}

			// Add events
			foreach (EventInfo ei in Framework.Do.GetEvents(type))
			{
				if (!ei.IsSpecialName && ShouldIncludeMember(ei.Name, ei, membersToIgnore))
				{
					AddMember(ei.Name, EventMemberDescriptor.TryCreateIfVisible(ei, this.AccessMode));
				}
			}

			// Add nested types
			foreach (Type nestedType in Framework.Do.GetNestedTypes(type))
			{
				if (ShouldIncludeMember(nestedType.Name, nestedType, membersToIgnore) &&
					!Framework.Do.IsGenericTypeDefinition(nestedType))
				{
					if (Framework.Do.IsNestedPublic(nestedType) ||
						Framework.Do.GetCustomAttributes(nestedType, typeof(MoonSharpUserDataAttribute), true).Length > 0)
					{
						var descr = UserData.RegisterType(nestedType, this.AccessMode);
						if (descr != null)
							AddDynValue(nestedType.Name, UserData.CreateStatic(nestedType));
					}
				}
			}

			// Handle array indexers
			if (!membersToIgnore.Contains("[this]"))
			{
				if (Type.IsArray)
				{
					int rank = Type.GetArrayRank();

					ParameterDescriptor[] get_pars = new ParameterDescriptor[rank];
					ParameterDescriptor[] set_pars = new ParameterDescriptor[rank + 1];

					for (int i = 0; i < rank; i++)
						get_pars[i] = set_pars[i] = new ParameterDescriptor("idx" + i.ToString(), typeof(int));

					set_pars[rank] = new ParameterDescriptor("value", Type.GetElementType());

					AddMember(SPECIALNAME_INDEXER_SET, new ArrayMemberDescriptor(SPECIALNAME_INDEXER_SET, true, set_pars));
					AddMember(SPECIALNAME_INDEXER_GET, new ArrayMemberDescriptor(SPECIALNAME_INDEXER_GET, false, get_pars));
				}
				else if (Type == typeof(Array))
				{
					AddMember(SPECIALNAME_INDEXER_SET, new ArrayMemberDescriptor(SPECIALNAME_INDEXER_SET, true));
					AddMember(SPECIALNAME_INDEXER_GET, new ArrayMemberDescriptor(SPECIALNAME_INDEXER_GET, false));
				}
			}
		}



		public void PrepareForWiring(Table t)
		{
			if (AccessMode == InteropAccessMode.HideMembers || Framework.Do.GetAssembly(Type) == Framework.Do.GetAssembly(this.GetType()))
			{
				t.Set("skip", DynValue.NewBoolean(true));
			}
			else
			{
				t.Set("visibility", DynValue.NewString(this.Type.GetClrVisibility()));

				t.Set("class", DynValue.NewString(this.GetType().FullName));
				DynValue tm = DynValue.NewPrimeTable();
				t.Set("members", tm);
				DynValue tmm = DynValue.NewPrimeTable();
				t.Set("metamembers", tmm);

				Serialize(tm.Table, Members);
				Serialize(tmm.Table, MetaMembers);
			}
		}

		private void Serialize(Table t, IEnumerable<KeyValuePair<string, IMemberDescriptor>> members)
		{
			foreach (var pair in members)
			{
				IWireableDescriptor sd = pair.Value as IWireableDescriptor;

				if (sd != null)
				{
					DynValue mt = DynValue.NewPrimeTable();
					t.Set(pair.Key, mt);
					sd.PrepareForWiring(mt.Table);
				}
				else
				{
					t.Set(pair.Key, DynValue.NewString("unsupported member type : " + pair.Value.GetType().FullName));
				}
			}
		}
	}
}

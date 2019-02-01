using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    class SubGraphOutputNode : ShaderNode, IHasSettings
    {
        [SerializeField]
        private List<InputDescriptor> m_InDescriptors = new List<InputDescriptor>();

        public ShaderStageCapability effectiveShaderStage
        {
            get
            {
                List<MaterialSlot> slots = new List<MaterialSlot>();
                GetInputSlots(slots);

                foreach(MaterialSlot slot in slots)
                {
                    ShaderStageCapability stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);

                    if(stage != ShaderStageCapability.All)
                        return stage;
                }

                return ShaderStageCapability.All;
            }
        }

        public SubGraphOutputNode()
        {
            name = "Output";
        }

#region Initialization
        internal override void Setup(ref NodeDefinitionContext context)
        {
            context.CreateNodeType(new NodeTypeDescriptor
            {
                path = "HIDDEN",
                name = "Subgraph Output",
                preview = false
            });
        }
#endregion

#region Validation
        private void ValidateShaderStage()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);

            foreach(MaterialSlot slot in slots)
                slot.stageCapability = ShaderStageCapability.All;

            var effectiveStage = effectiveShaderStage;

            foreach(MaterialSlot slot in slots)
                slot.stageCapability = effectiveStage;
        }

        public override void ValidateNode()
        {
            List<int> validShaderValues = new List<int>();
            for(int i = 0; i < m_InDescriptors.Count; i++)
            {
                IShaderValueDescriptor descriptor = m_InDescriptors[i];
                ValidateDescriptor(ref descriptor);
                AddShaderValue(descriptor, ShaderValueDescriptorType.Input);
                validShaderValues.Add(descriptor.id);
                m_InDescriptors[i] = (InputDescriptor)descriptor;
            }
            RemoveShaderValuesNotMatching(validShaderValues);

            ValidateShaderStage();
            base.ValidateNode();
        }

        private void ValidateDescriptor(ref IShaderValueDescriptor descriptor)
        {
            if(descriptor.id != -1)
                return;

            int idCeiling = -1;
            int duplicateNameCeiling = 0;
            var shaderValues = GetShaderValues();
            foreach(IShaderValue value in shaderValues)
            {
                idCeiling = value.id > idCeiling ? value.id : idCeiling;
                if(value.displayName.StartsWith("New"))
                    duplicateNameCeiling++;
            }
            descriptor.id = idCeiling + 1;
            descriptor.name = "New";
            if(duplicateNameCeiling > 0)
                descriptor.name += string.Format(" ({0})", duplicateNameCeiling);
        }
#endregion

#region Slots
        public virtual int AddSlot()
        {
            var index = this.GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new Vector4MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Input, Vector4.zero));
            return index;
        }
#endregion

#region Outputs
        public void RemapOutputs(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var slot in graphOutputs)
                visitor.AddShaderChunk(string.Format("{0} = {1};", slot.shaderOutputName, GetSlotValue(slot.id, generationMode)), true);
        }

        public IEnumerable<MaterialSlot> graphOutputs
        {
            get
            {
                return NodeExtensions.GetInputSlots<MaterialSlot>(this).OrderBy(x => x.id);
            }
        }
#endregion

#region Views
        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();
            ps.style.width = 400;
            ps.Add(new InputDescriptorListView(this, m_InDescriptors, ShaderValueDescriptorType.Input));
            return ps;
        }
#endregion

    }
}

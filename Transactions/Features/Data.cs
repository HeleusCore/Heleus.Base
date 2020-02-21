using System;
using System.Collections.Generic;
using System.Text;
using Heleus.Base;

namespace Heleus.Transactions.Features
{
    public enum DataError
    {
        None,
        Empty,
        InvalidItem
    }

    public enum DataTypes
    {
        Long,
        Double,
        String,
        Binary,
    }

    public class Data : FeatureData
    {
        public new const ushort FeatureId = 9;

        public int Count => Items.Count;

        public readonly Dictionary<short, DataItem> Items = new Dictionary<short, DataItem>();

        public Data(Feature feature) : base(feature)
        {
        }

        public Data AddLong(short index, long data)
        {
            Items[index] = new DataItem(index, DataTypes.Long, BitConverter.GetBytes(data));
            return this;
        }

        public Data AddDouble(short index, double data)
        {
            Items[index] = new DataItem(index, DataTypes.Double, BitConverter.GetBytes(data));
            return this;
        }

        public Data AddString(short index, string data)
        {
            Items[index] = new DataItem(index, DataTypes.String, Encoding.UTF8.GetBytes(data));
            return this;
        }

        public Data AddBinary(short index, byte[] data)
        {
            Items[index] = new DataItem(index, DataTypes.Binary, data);
            return this;
        }

        public override void PackTransactionData(Packer packer)
        {
            base.PackTransactionData(packer);
            packer.Pack(Items);
        }

        public override void UnpackTransactionData(Unpacker unpacker, ushort size)
        {
            base.UnpackTransactionData(unpacker, size);
            unpacker.Unpack(Items, (u) => new DataItem(u));
        }

        public bool HasItem(short index)
        {
            return Items.ContainsKey(index);
        }

        public DataItem GetItem(short index)
        {
            Items.TryGetValue(index, out var item);
            return item;
        }

        public bool GetItem(short index, out DataItem dataItem)
        {
            return Items.TryGetValue(index, out dataItem);
        }
    }

    public class DataItem : IPackable, IUnpackerKey<short>
    {
        public bool IsValid => Data != null;

        public short UnpackerKey => Index;

        public readonly short Index;
        public readonly DataTypes DataType;
        public readonly byte[] Data;
        public int Length => Data.Length;

        public DataItem(short index, DataTypes dataType, byte[] data)
        {
            Index = index;
            DataType = dataType;
            Data = data;
        }

        public DataItem(Unpacker unpacker)
        {
            DataType = (DataTypes)unpacker.UnpackByte();

            if (DataType == DataTypes.Long || DataType == DataTypes.Double)
            {
                Data = unpacker.UnpackByteArray(8);
            }
            else
            {
                Data = unpacker.UnpackByteArray();
            }
        }

        public long NumberValue
        {
            get
            {
                CheckDataType(DataTypes.Long);
                return BitConverter.ToInt64(Data, 0);
            }
        }

        public double DecimalValue
        {
            get
            {
                CheckDataType(DataTypes.Double);
                return BitConverter.ToDouble(Data, 0);
            }
        }

        public string StringValue
        {
            get
            {
                CheckDataType(DataTypes.String);
                if (Data == null)
                    return null;

                return Encoding.UTF8.GetString(Data);
            }
        }

        public byte[] BinaryValue
        {
            get
            {
                CheckDataType(DataTypes.Binary);
                return Data;
            }
        }

        void CheckDataType(DataTypes targetType)
        {
            if (targetType != DataType)
                throw new ArgumentException("Invalid DataType.");
        }

        public void Pack(Packer packer)
        {
            packer.Pack((byte)DataType);
            if (DataType == DataTypes.Long || DataType == DataTypes.Double)
            {
                packer.Pack(Data, 8);
            }
            else
            {
                packer.Pack(Data);
            }
        }
    }

    public class DataValidator : FeatureDataValidator
    {
        public DataValidator(Feature feature, IFeatureChain currentChain) : base(feature, currentChain)
        {
        }

        public override (bool, int) Validate(Transaction transaction, FeatureData featureData)
        {
            var error = DataError.None;
            var data = featureData as Data;

            var items = data.Items;
            if (items.Count == 0)
            {
                error = DataError.Empty;
                goto end;
            }

            foreach (var item in items.Values)
            {
                if (!item.IsValid)
                {
                    error = DataError.InvalidItem;
                    goto end;
                }
            }

        end:
            return (error == DataError.None, (int)error);
        }
    }

    public class DataFeature : Feature
    {
        public DataFeature() : base(Data.FeatureId, FeatureOptions.HasTransactionData | FeatureOptions.RequiresDataValidator)
        {
            ErrorEnumType = typeof(DataError);
        }

        public override FeatureData NewFeatureData()
        {
            return new Data(this);
        }

        public override FeatureDataValidator NewValidator(IFeatureChain currentChain)
        {
            return new DataValidator(this, currentChain);
        }

        public override FeatureAccountContainer NewAccountContainer(FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureChainHandler NewChainHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureMetaDataProcessor NewProcessor()
        {
            throw new NotImplementedException();
        }

        public override FeatureQueryHandler NewQueryHandler(IFeatureChain currentChain)
        {
            throw new NotImplementedException();
        }

        public override FeatureAccountContainer RestoreAccountContainer(Unpacker unpacker, ushort size, FeatureAccount featureAccount)
        {
            throw new NotImplementedException();
        }

        public override FeatureRequest RestoreRequest(Unpacker unpacker, ushort size, ushort requestId)
        {
            throw new NotImplementedException();
        }
    }
}

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace GeometryReasoning
{
    internal class DataInterface
    {

        //zhiliang shi add

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DIRECTION
        {
            public double x;
            public double y;
            public double z;
        };

        [DllImport("GeoreasonInterface.dll", EntryPoint = "InitializeSat", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool InitializeSat(string filename);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "NumberOfParts", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int NumberOfParts();

        [DllImport("GeoreasonInterface.dll", EntryPoint = "NameOfPart", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern IntPtr NameOfPart(int i);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "VerticesOfPart", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int VerticesOfPart(int i);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "GetPointDouble", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void GetPointDouble(int len, [MarshalAs(UnmanagedType.LPArray)] double[] x);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Terminate_Facet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool Terminate_Facet();

        [DllImport("GeoreasonInterface.dll", EntryPoint = "CheckRelationOne", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void CheckRelationOne(string f_part, string s_part, ref int flag);


        [DllImport("GeoreasonInterface.dll", EntryPoint = "CheckRelationTwo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool CheckRelationTwo(string f_part, string s_part);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_Dof", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_Dof(int vector_index, int index, ref DIRECTION dir);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_DofVector_len", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_DofVector_len(int index, ref int len);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_Dof_DBP_RowNum", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_Dof_DBP_RowNum(int index, ref int rownum);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_Dof_DBP_ColumnNum", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_Dof_DBP_ColumnNum(int index, int rownum, ref int columnnum);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_Dof_DBP", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_Dof_DBP(int index, int rowindex, int columnindex, ref int DBP_data);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_invisibleDistance_size", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_invisibleDistance_size(ref int size_dis);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_invisibleDistance_data", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_invisibleDistance_data(int index, ref double data_dis);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Output_clashtype", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void Output_clashtype(ref int clashtype);

        [DllImport("GeoreasonInterface.dll", EntryPoint = "Terminate", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool Terminate();

        //

        public int ClashClassType = -1;
        public List<DIRECTION> visibleDOF;
        public List<List<int>> visibleDOF_DBP;
        public List<DIRECTION> invisibleDOF;
        public List<List<int>> invisibleDOF_DBP;
        public List<double> invisibleDistance;

        public List<DIRECTION> concentricDOF;
        public List<List<int>> concentricDOF_DBP;

        public List<double[]> Evaluation_Times; // this is an array of three times:orientation , insertion , and handling Time
        public List<double[]> clashLocation; // not sure how this is used.


        internal static void Initialize(string filename)
        {

            InitializeSat(filename);
        }

        internal static void TerminateACIS()
        {
            try
            {
                Terminate();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Unable to stop ACIS because: " + exception.Message);
            }
        }

        internal static int failures, successes;
        public static DataInterface MakeDataInterface(string movingName, string refName)
        {
            var dataInterface = new DataInterface();

            /*************** here's the connection to C++ ****************/
            Console.Write("CheckRelationTwo...");
            if (!CheckRelationTwo(movingName, refName))
            {                            
            Console.WriteLine("done");
                failures++;
                return null;
            }                            
            Console.WriteLine("done");
            successes++;

            //output visibleDOF  invisibleDOF   concentricDOF
            DIRECTION dir;
            dataInterface.visibleDOF= new List<DIRECTION>();
            dataInterface.visibleDOF_DBP=new List<List<int>>();
            dataInterface.invisibleDOF=new List<DIRECTION>();
            dataInterface.invisibleDOF_DBP=new List<List<int>>();
            dataInterface.invisibleDistance= new List<double>();
            dataInterface.concentricDOF=new List<DIRECTION>();
            dataInterface.concentricDOF_DBP=new List<List<int>>();
            dataInterface.Evaluation_Times= new List<double[]>();
            dataInterface.clashLocation= new List<double[]>();
   

            dir.x = 0.0; dir.y = 0.0; dir.z = 0.0;
            int vectorlen = 0;
            for (int i = 0; i < 3; i++)
            {
                Output_DofVector_len(i, ref vectorlen);

                if (i == 0 && vectorlen != 0)
                {
                    for (int j = 0; j < vectorlen; j++)
                    {
                        Output_Dof(i, j, ref dir);
                        dataInterface.visibleDOF.Add(dir);
                        //d.visibleDOF[j].x = dir.x; d.visibleDOF[j].y = dir.y; d.visibleDOF[j].z = dir.z;//output visibleDOF
                    }
                }
                else if (i == 1 && vectorlen != 0)
                {
                    for (int z = 0; z < vectorlen; z++)
                    {
                        Output_Dof(i, z, ref dir);
                        dataInterface.invisibleDOF.Add(dir);
                        // d.invisibleDOF[z].x = dir.x; d.invisibleDOF[z].y = dir.y; d.invisibleDOF[z].z = dir.z;//output invisibleDOF
                    }
                }
                else if (i == 2 && vectorlen != 0)
                {
                    for (int q = 0; q < vectorlen; q++)
                    {
                        Output_Dof(i, q, ref dir);
                        dataInterface.concentricDOF.Add(dir);
                        // d.concentricDOF[q].x = dir.x; d.concentricDOF[q].y = dir.y; d.concentricDOF[q].z = dir.z;//output concentricDOF
                    }

                }
            }
            // //output visibleDOF_DBP  invisibleDOF_DBP   concentricDOF_DBP
            int rownum = 0;
            int columnnum = 0;
            int DBP_data = 0;
            for (int i = 0; i < 3; i++)
            {
                Output_Dof_DBP_RowNum(i, ref rownum);
                if (rownum != 0)
                {
                    for (int j = 0; j < rownum; j++)
                    {
                        var columnList = new List<int>();
                        if (i == 0)
                        {
                            Output_Dof_DBP_ColumnNum(i, j, ref columnnum);
                            if (columnnum != 0)
                            {
                                for (int q = 0; q < columnnum; q++)
                                {
                                    Output_Dof_DBP(i, j, q, ref DBP_data);
                                    columnList.Add(DBP_data);
                                }
                            }
                            dataInterface.visibleDOF_DBP.Add(columnList);
                        }
                        else if (i == 1)
                        {
                            Output_Dof_DBP_ColumnNum(i, j, ref columnnum);
                            if (columnnum != 0)
                            {
                                for (int q = 0; q < columnnum; q++)
                                {
                                    Output_Dof_DBP(i, j, q, ref DBP_data);
                                    columnList.Add(DBP_data);
                                    //dataInterface.invisibleDOF_DBP[j][q] = DBP_data;
                                }
                            }
                            dataInterface.invisibleDOF_DBP.Add(columnList);
                        }
                        else
                        {
                            Output_Dof_DBP_ColumnNum(i, j, ref columnnum);
                            if (columnnum != 0)
                            {
                                for (int q = 0; q < columnnum; q++)
                                {
                                    Output_Dof_DBP(i, j, q, ref DBP_data);
                                    columnList.Add(DBP_data);
                                    //dataInterface.concentricDOF_DBP[j][q] = DBP_data;
                                }
                            }
                            dataInterface.concentricDOF_DBP.Add(columnList);
                        }
                    }
                }
            }

            //// //output invisibleDistance
            int dis_len = 0;
            double data_dis = 0.0;
            Output_invisibleDistance_size(ref dis_len);
            for (int i = 0; i < dis_len; i++)
            {
                Output_invisibleDistance_data(i, ref data_dis);
                //dataInterface.invisibleDistance[i] = data_dis;
                dataInterface.invisibleDistance.Add(data_dis);
            }
            //output clashtype
            int checkclashtype = 0;
            Output_clashtype(ref checkclashtype);
            dataInterface.ClashClassType = checkclashtype;

         //   bool log_stop = Terminate();
            //******************************************************************
            return dataInterface;
        }
    }
}


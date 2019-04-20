/* 
**************************************
* Popo Acount: 
* Version: 1.0
* UnityVersion: 2017.1.2p2
* Date: 12/10/2018
* Description: 
**************************************
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

/// <summary>
/// 自动合并工具
/// </summary>
public class MergeTool
{
	//只输出log不改变任何文件？
	public bool OnlyLog = true;
	//Log输出文件路径
	public string LogPath = "./MergeToolLog.txt";
	//是否只文件夹对比不做文件详细合并
	public bool OnlyFoderCompare = false;
	//排除目录，在这个目录的不合并,分号分隔
	public string ExceptFolder = "";
	//指定目录，不在这个目录的不合并,分号分隔
	public string SpecifyFolder = "";
	//排除后缀
	public string ExceptExtension = "";
	//指定后缀
	public string SpecifyExtension = "";
	//向上容错行
	public int UpNeedNotDirtyRow = 3;
	//向下容错行
	public int DownNeedNotDirtyRow = 2;
	//每个文件的对比回调，可以直接获取对比进度和对比总数
	public delegate void CompareFunction(int count, int idx, string name);
	public event CompareFunction CompareCallBack;
	//每个文件内每行的对比回调，可以直接获取对比进度和对比总数
	public event CompareFunction CompareDetailCallBack;

	static MergeTool s_Instance;

    public int ReplaceFileCount { protected set; get; }
    public int InsertFileCount { protected set; get; }
    public int RemoveFileCount { protected set; get; }

    public int ReplaceLineCount { protected set; get; }
    public int InsertLineCount { protected set; get; }
    public int RemoveLineCount { protected set; get; }

    public Dictionary<string, Dictionary<int, List<int>>> FaildFileResult { protected set; get; }


    public static MergeTool Instance
	{
		get
		{
			if (s_Instance == null)
				s_Instance = new MergeTool();
			return s_Instance;
		}

	}

	private MergeTool() { }

	#region XML目录数据结构
	/// <summary>
	/// 文件对比根节点
	/// 
	/// Namespace是个坑，如果节点命名是X:Y，那么说明一定是需要命名空间，
	/// 且这时候xml文件里必须定义xmlns:mg="http://www.araxis.com/2002/Merge/Reporting"才能使用Namespace，
	/// XmlRoot也必须定义命名空间属性Namespace =,并且Namespace =必须是后面那段地址
	/// 
	/// 不想序列化的节点不加到成员变量里就行了，此处就没加上mg:metaData进去
	/// 
	/// 如果节点是文本的话必须要加XmlText
	/// 
	/// 都必须是public，否则无法反序列化
	/// </summary>
	[XmlRoot("report", Namespace = "http://www.araxis.com/2002/Merge/Reporting")]
	public class MGReport
	{
		//是属性的地方XmlAttribute必须加上，没有连下去一说，默认是XmlElement
		[XmlAttribute]
		public string version;
		[XmlAttribute]
		public string type;
		public MGMetaData metaData;
		//父节点里的成员变量名字必须要完全对应XML文件里的，如果不对应就在父节点成员变量里用XmlElement来修改,而且数组[]必须要加XmlElement，不是数组可以不加
		public MGRowData rowData;
	}

	/// <summary>
	/// 表格元数据节点
	/// </summary>
	public class MGMetaData
	{
		[XmlElement("folderDetails")]
		public MGFolderDetails[] folderDetails;
	}

	/// <summary>
	/// 目录路路径节点
	/// </summary>
	public class MGFolderDetails
	{
		[XmlAttribute]
		public int folder;
		public string name;
		public string location;
	}

	/// <summary>
	/// 行数据容器节点
	/// 作为子节点，不需要再用XmlRoot来改名字或者声明Namespace了，但是父节点里的成员变量名字必须要完全对应XML文件里的，如果不对应就在父节点成员变量里用XmlElement来修改
	/// </summary>
	public class MGRowData
	{
		//父节点里的成员变量名字必须要完全对应XML文件里的，如果不对应就在父节点成员变量里用XmlElement来修改,而且数组[]必须要加XmlElement
		[XmlElement("row")]
		public MGRow[] rows;
	}

	/// <summary>
	/// 行数据节点
	/// </summary>
	public class MGRow
	{
		[XmlAttribute]
		public string url;                          //文件详细对比报告的路径
		[XmlAttribute]
		public int reportId;                            //文件详细对比报告的索引，有路径就够了这个没用到
		[XmlAttribute]
		public bool selected;                           //是否被选中，在操作AM的时候就手动进行设置好了，这里用不到
		[XmlElement("file")]
		public MGFile[] files;
	}

	/// <summary>
	/// 文件节点
	/// </summary>
	public class MGFile
	{
		[XmlAttribute]
		public string name;                         //文件或目录名
		[XmlAttribute]
		public int folder;                          //所在对比的目录路索引
		[XmlAttribute]
		public string size;                         //文件或目录大小
		[XmlAttribute]
		public string timestamp;                        //不知道啥的时间戳，用不到
		[XmlAttribute("class")]
		public string modifyClass;                      //修改类型：“U”没变化，“C”更改，“I”添加，“R”删除
		[XmlAttribute]
		public int level;                               //所在目录层级
		[XmlAttribute]
		public string changeCount;                     //跟相邻目录路对比有改变的数量
		[XmlAttribute]
		public string icon;                         //用来显示的icon名，可以用来辅助判断是否是目录

		public int ChangeCount
		{
			get
			{
				int num;
				if (int.TryParse(changeCount, out num))
				{
					return num;
				}
				else
				{
					return 999; //changeCount = "≥1"的情况
				}
			}
		}
	}
	#endregion



	#region XML文件数据结构
	/// <summary>
	/// 详细对比根节点
	/// </summary>
	[XmlRoot("report", Namespace = "http://www.araxis.com/2002/Merge/Reporting")]
	public class DReport
	{
		[XmlAttribute]
		public string type;
		public DMetaData metaData;
		public DRowData rowData;
	}

	/// <summary>
	/// 表格元数据节点
	/// </summary>
	public class DMetaData
	{
		[XmlElement("fileDetails")]
		public DFolderDetails[] fileDetails;
	}

	/// <summary>
	/// 文件路路径节点
	/// </summary>
	public class DFolderDetails
	{
		[XmlAttribute]
		public int file;
		public string name;
		public string location;
	}

	/// <summary>
	/// 行数据容器节点
	/// </summary>
	public class DRowData
	{
		[XmlElement("rowGroup")]
		public DRowGroup[] rowGroups;
	}

	/// <summary>
	/// 对比组节点，横向一块颜色相当于一个对比组节点，包括没变化的白色也是
	/// </summary>
	public class DRowGroup
	{
		[XmlElement("row")]
		public DRow[] rows;
	}

	/// <summary>
	/// 行数据节点，单单一行而已
	/// </summary>
	public class DRow
	{
		[XmlElement("ln")]
		public DLine[] lines;

		public DLine GetLine(int lineIdx)
		{
			foreach (var line in lines)
			{
				if (line.file == lineIdx)
					return line;
			}
			return null;
		}
	}

	/// <summary>
	/// 路数据节点，每一行里各路的数据
	/// </summary>
	public class DLine
	{
		[XmlAttribute]
		public int file;                                //所在对比的目录路索引
		[XmlAttribute]
		public int number;                          //行号
		[XmlAttribute("class")]
		public string modifyClass;                      //修改类型：“U”没变化，“C”更改，“I”添加，“R”删除
		[XmlElement("sg")]
		public DSegment[] segments;                     //片段，所有内容都是由片段组成的

		public string GetText()
		{
			string text = "";
			foreach (var segment in segments)
			{
				if (segment.texts != null)
				{
					foreach (var tx in segment.texts)
						text += tx;
				}
			}
			return text;
		}
	}

	/// <summary>
	/// 路数据节点，每一行里各路的数据
	/// </summary>
	public class DSegment
	{
		[XmlAttribute("class")]
		public string modifyClass;                      //修改类型：“U”没变化，“C”更改，“I”添加，“R”删除
		[XmlElement("s")]
		public DSpace[] spaces;                     //空格，用不上
		[XmlElement("lf")]
		public DLF[] lfs;                               //换行，用不上
		[XmlText]                                       //如果节点是文本的话必须要加XmlText
		public string[] texts;                          //文本
	}

	/// <summary>
	/// 空格节点，描述空格
	/// </summary>
	public class DSpace
	{

	}

	/// <summary>
	/// 换行节点，描述换行
	/// </summary>
	public class DLF
	{

	}
	#endregion

	enum ERowCompareResultType
	{
		FAILED,
		FAILED_CROSSJUGE,
		SUCCEED,
		SUCCEED_CROSSJUGE,
	}

	class CompareDetailParam
	{
		public List<string> LineList1 = new List<string>();
		public List<string> LineList2 = new List<string>();
		public List<string> LineList3 = new List<string>();

		//因为不是每一行最左边的路都有行号，比如插入操作，所以记录上一次能获取的行数
		public int HandleLineNum = 0;
		//因为不是每一行最左边的路的行号都是准确的，比如插入和删除后行号还是之前的行号，但其实已经新增和删除了，所以记录下偏移量
		public int OffsetLineNum = 0;
		//当前的总行数
		public int RealRowIdx = -1;
		//记录连续可合并和无需合并的Row数，用来判断是否需要合并
		public int CurrentNotDirtyUpRowNum = 0;
		//上面连续可合并和无需合并的Row超过NeedNotDirtyRowNum行才可以合并
		public int NeedNotDirtyUpRowNum = 3;
		//下面连续可合并和无需合并的Row超过NeedNotDirtyDownRowNum行才可以合并
		public int NeedNotDirtyDownRowNum = 2;

		public int GetRealHandleLineNum()
		{
			return HandleLineNum + OffsetLineNum;
		}

		/// <summary>
		/// 从对比的文件获取行数据，而不是从解析xml里获取，用来替换的
		/// </summary>
		/// <param name="lineList1"></param>
		/// <param name="lineList2"></param>
		/// <param name="lineList3"></param>
		/// <param name="fileIdx"></param>
		/// <param name="number"></param>
		/// <returns></returns>
		public string GetRealLineText(int fileIdx, int number)
		{
			List<string> lineList = null;
			if (fileIdx == 1)
				lineList = LineList1;
			else if (fileIdx == 2)
				lineList = LineList2;
			else if (fileIdx == 3)
				lineList = LineList3;

            return "";//lineList[number - 1];
		}

		public bool CheckCanMergeByDirtyRow()
		{
			if (RealRowIdx < NeedNotDirtyUpRowNum)
				return true;
			else
				if (CurrentNotDirtyUpRowNum >= NeedNotDirtyUpRowNum)
					return true;
			return false;
		}

		/// <summary>
		/// 根据当前groupIdx，rowIdx获取下第nextNumIdx个row并返回那个row的groupIdx，rowIdx
		/// </summary>
		/// <param name="detailRoot"></param>
		/// <param name="groupIdx"></param>
		/// <param name="rowIdx"></param>
		/// <param name="nextIdx"></param>
		/// <returns></returns>
		public DRow GetNextRow(DReport detailRoot, ref int groupIdx, ref int rowIdx, int nextNumIdx)
		{
			for (; groupIdx < detailRoot.rowData.rowGroups.Length; ++groupIdx)
			{
				var dgroup = detailRoot.rowData.rowGroups[groupIdx];
				for (; rowIdx < dgroup.rows.Length; ++rowIdx)
				{
					var drow = dgroup.rows[rowIdx];
					if (nextNumIdx <= 0)
						return drow;
					nextNumIdx--;
				}
				rowIdx = 0;
			}
			return null;
		}

	}

	/// <summary>
	/// 从XML文件获取行数据，而不是从要对比的文件里获取，用来对比的，不能用来替换，因为空格之类的符号去掉了
	/// </summary>
	/// <param name="detailRoot"></param>
	/// <param name="startGroupIdx"></param>
	/// <param name="lineIdx"></param>
	/// <param name="lineNumber"></param>
	/// <param name="findGoupIdx"></param>
	/// <param name="findRowIdx"></param>
	/// <returns></returns>
	string GetTextByLineNumber(DReport detailRoot, int startGroupIdx, int lineIdx, int lineNumber,
		out int findGoupIdx, out int findRowIdx)
	{
		string result = null;
		findGoupIdx = 0;
		findRowIdx = 0;
		for (int groupIdx = startGroupIdx; groupIdx < detailRoot.rowData.rowGroups.Length; ++groupIdx)
		{
			var dgroup = detailRoot.rowData.rowGroups[groupIdx];
			for (int rowIdx = 0; rowIdx < dgroup.rows.Length; ++rowIdx)
			{
				var drow = dgroup.rows[rowIdx];
				var dline = drow.GetLine(lineIdx);
				if (dline == null) continue;

				if (dline.number == lineNumber)
				{
					result = dline.GetText();
					findGoupIdx = groupIdx;
					findRowIdx = rowIdx;
					return result;
				}
			}
		}

		return result;
	}

	/// <summary>
	/// 有的1、3路本来Goup应该是相等的，但是AM却把他们的Goup切成不一样的了，这时候需要递归判断,如果成功后取出最后的行号，以便外部不用再判断中间判断过的了
	/// </summary>
	/// <param name="detailRoot"></param>
	/// <param name="firstLineIdx"></param>
	/// <param name="groupIdx"></param>
	/// <param name="toRow"></param>
	/// <param name="toGroup"></param>
	/// <returns></returns>
	bool CrossJuge(DReport detailRoot, int startLineIdx, int startGroupIdx, int startRowIdx, int opGroupIdx, int opStartRowIdx, ref HashSet<DRow> crossJugeRowList)
	{
		bool result = false;

		int opLineIdx = 1;
		if (startLineIdx == 1) opLineIdx = 3;

		//取出交叉判断的第一个Goup的第一Row的行索引
		var startGroup = detailRoot.rowData.rowGroups[startGroupIdx];
		DLine opOneLine = null;
        //尝试10次,因为对面的路不一定在下一个Goup里，下一个Goup里可能只有中间路的而不一定是两边路的,
        //(XXX)这时候就从下一个Group找，这样最多找10次直到找到为止(XXX)
        //上面的判断会有bug,2019-4-20 更新，因为只是该Goup的这一行只有中间路两边路没有而已，该Goup的下面几行还有会可能有两边路的，所以先循环该Goup的所有行没有再到下一个Goup
		for (int i = 0; i < 10; ++i)
		{
			opGroupIdx += i;
			if (opGroupIdx < detailRoot.rowData.rowGroups.Length)
			{
				var opGroup = detailRoot.rowData.rowGroups[opGroupIdx];
                for (;opStartRowIdx < opGroup.rows.Length; ++ opStartRowIdx)
                {
                    var opOneRow = opGroup.rows[opStartRowIdx];
                    opOneLine = opOneRow.GetLine(opLineIdx);

                    if (opOneLine != null)
                        break;
                }
			}
			if (opOneLine != null)
				break;

            opStartRowIdx = 0;
        }
		if (opOneLine == null) return result;
		var opOneNum = opOneLine.number;

		var opRowIdx = 0;
		for (int rowIdx = startRowIdx; rowIdx < startGroup.rows.Length; ++rowIdx)
		{
			//该Goup从Row为0开始往下判断
			var drow = startGroup.rows[rowIdx];
			var line = drow.GetLine(startLineIdx);
			//这里line有可能为空，因为这一Goup有可能中间路多很多Row，多出的那些Row两边都可能没有，这些Row都是新增Line操作的，所以直接crossJugeRowList.Add(drow);就可以了
			if (line != null)
			{
				var opText = GetTextByLineNumber(detailRoot, opGroupIdx, opLineIdx, opOneNum, out opGroupIdx, out opRowIdx);
				if (opText == null) return result;
				if (line.GetText() != opText) return result;
				opOneNum++;
			}
			crossJugeRowList.Add(drow);
			crossJugeRowList.Add(detailRoot.rowData.rowGroups[opGroupIdx].rows[opRowIdx]);
		}

		//先直接往下做判断，只有往下没有当前路的行或当前路的行没有改变的才做CrossJuge，否则就NextJuge
		bool isNextJuge = false;
		var nextGroup = startGroupIdx + 1;
		if (detailRoot.rowData.rowGroups.Length > nextGroup)
		{
			var group = detailRoot.rowData.rowGroups[nextGroup];
			if (group != null)
			{
				var line = group.rows[0].GetLine(startLineIdx);
				if (line != null)
				{
					//如果3路里有其中一路有变化才做对比
					bool needHandl = false;
					foreach (var dline in group.rows[0].lines)
					{
						if (dline.modifyClass != "U" && dline.modifyClass != "UU")
						{
							needHandl = true;
							break;
						}
					}
					if (needHandl)
					{
						isNextJuge = true;
						//如果已经满了，就开始从下一Goup的0开始
						if (opRowIdx == detailRoot.rowData.rowGroups[opGroupIdx].rows.Length - 1)
						{
							opGroupIdx++;
							opRowIdx = 0;
						}
						//否则就在当前的Goup的下一个Row开始
						else
							opRowIdx++;
						return CrossJuge(detailRoot, startLineIdx, nextGroup, 0, opGroupIdx, opRowIdx, ref crossJugeRowList);
					}
				}
			}
		}
		

		if (isNextJuge == false)
		{
			if (opRowIdx == detailRoot.rowData.rowGroups[opGroupIdx].rows.Length - 1)
			{
				result = true;
			}
			else
				return CrossJuge(detailRoot, opLineIdx, opGroupIdx, opRowIdx + 1, startGroupIdx + 1, 0, ref crossJugeRowList);
		}

		return result;
	}

	void InsertFile(string sourceFileName, string destFileName)
	{
		DebugLog("InsertFile:" + "sourceFileName:" + sourceFileName + "    destFileName:" + destFileName);
		if (!OnlyLog)
		{
			//判断是否是目录
			if (Directory.Exists(sourceFileName) == true)
			{
				if (Directory.Exists(destFileName) == false)
					Directory.CreateDirectory(destFileName);
			}
			else
			{
				File.Copy(sourceFileName, destFileName, true);
			}
		}
        InsertFileCount++;
	}

	void RemoveFile(string fileName)
	{
		DebugLog("RemoveFile:" + "fileName:" + fileName);
		if (!OnlyLog)
		{
			//判断是否是目录
			if (Directory.Exists(fileName) == true)
			{
				if (Directory.Exists(fileName) == true)
					Directory.Delete(fileName, true);
			}
			else
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
			}
		}
        RemoveFileCount++;
	}

	void ReplaceFile(string sourceFileName, string destFileName)
	{
		DebugLog("ReplaceFile:" + "sourceFileName:" + sourceFileName + "    destFileName:" + destFileName);
		if (!OnlyLog)
			File.Copy(sourceFileName, destFileName, true);
        ReplaceFileCount++;
	}

	void InsertLine(ref List<string> lineList, int insertLineIdx, string insertText)
	{
		if (insertText.IndexOf("\r\n") < 0) insertText += "\r\n";
        // 		DebugLog("InsertLine:" + "insertLineIdx:" + insertLineIdx + "    insertText:" + insertText);
        // 		lineList.Insert(insertLineIdx, insertText);
        InsertLineCount++;
	}

	void RemoveLine(ref List<string> lineList, int removeLineIdx)
	{
        // 		DebugLog("RemoveLine:" + "removeLineIdx:" + removeLineIdx + "    beRemoveLineIdx:" + lineList[removeLineIdx - 1]);
        // 		lineList.RemoveAt(removeLineIdx - 1);
        RemoveLineCount++;
	}

	void ReplaceLine(ref List<string> lineList, int replaceLineIdx, string replaceText)
	{
		if (replaceText.IndexOf("\r\n") < 0) replaceText += "\r\n";
        // 		DebugLog("ReplaceLine:" + "replaceLineIdx:" + replaceLineIdx + "    beReplaceText:" + lineList[replaceLineIdx - 1] + "    replaceText:" + replaceText);
        // 		lineList[replaceLineIdx - 1] = replaceText;
        ReplaceLineCount++;
	}

	void GetFileLine(ref List<string> lineList, string fileName)
	{
		if (File.Exists(fileName))
		{
			StreamReader sr = new StreamReader(fileName, true);
			while (!sr.EndOfStream)
			{
				string sTmp = sr.ReadLine();
				lineList.Add(sTmp + "\r\n");
			}
			sr.Close();
		}
	}

	void SaveFileLine(ref List<string> lineList, string fileName)
	{
		if (!OnlyLog)
		{
			string text = "";
			lineList.ForEach(lineData => text = text + lineData);
			StreamWriter sw = new StreamWriter(fileName, false);
			sw.Write(text);
			sw.Flush();
			sw.Close();
		}
	}

    void SaveResult()
    {
        DebugLog("");
        DebugLog("");
        DebugLog("----------------------------------------------------------------");
        DebugLog("Result:");
        DebugLog("");
        DebugLog("ReplaceFileCount:" + ReplaceFileCount);
        DebugLog("InsertFileCount:" + InsertFileCount);
        DebugLog("RemoveFileCount:" + RemoveFileCount);
        DebugLog("ReplaceLineCount:" + ReplaceLineCount);
        DebugLog("InsertLineCount:" + InsertLineCount);
        DebugLog("RemoveLineCount:" + RemoveLineCount);
        DebugLog("");
        DebugLog("");
        DebugLog("----------------------------------------------------------------");
        int NeedHandleChunkCount = 0;
        foreach (var fr in FaildFileResult)
        {
            foreach (var l in fr.Value)
            {
                NeedHandleChunkCount++;
            }
        }
        DebugLog("Need Handle FileCount:" + FaildFileResult.Count);
        DebugLog("Need Handle ChunkCount:" + NeedHandleChunkCount);
        DebugLog("");
        foreach (var fr in FaildFileResult)
        {
            string fileName = fr.Key;
            string line = "";
            foreach (var l in fr.Value)
            {
                line += l.Key + "; ";
            }
            DebugLog("HandleFileName: " + fileName);
            DebugLog("HandleLineStart: " + line);
            DebugLog("");
        }
    }

    /// <summary>
    /// 这个不单单是合并，在合并时还会判断是否可以合并，而是否可以合并的条件又包含需要向上和向下判断是否Compare成功，所以还需要
    /// DReport detailRoot, ref CompareDetailParam param, int groupIdx, int rowIdx, ref HashSet<DRow> crossJugeRowList这一坨参数
    /// </summary>
    /// <param name="resultType"></param>
    /// <param name="drow"></param>
    /// <param name="detailRoot"></param>
    /// <param name="param"></param>
    /// <param name="groupIdx"></param>
    /// <param name="rowIdx"></param>
    /// <param name="crossJugeRowList"></param>
    void MergeRow(ERowCompareResultType resultType, DRow drow, DReport detailRoot, ref CompareDetailParam param, int groupIdx, int rowIdx, ref HashSet<DRow> crossJugeRowList)
	{
		switch (resultType)
		{
			case ERowCompareResultType.FAILED:
			case ERowCompareResultType.FAILED_CROSSJUGE:
				{
					param.CurrentNotDirtyUpRowNum = 0;
                    string fileName = detailRoot.metaData.fileDetails[0].location + "\\" + detailRoot.metaData.fileDetails[0].name;
                    int line = param.GetRealHandleLineNum();
                    Dictionary<int, List<int>> lineResult;
                    if (!FaildFileResult.TryGetValue(fileName,out lineResult))
                    {
                        lineResult = new Dictionary<int, List<int>>();
                        FaildFileResult.Add(fileName, lineResult);
                    }
                        
                    List<int> lineData;
                    if (!lineResult.TryGetValue(line, out lineData))
                    {
                        bool isNew = true;
                        if (lineResult.Count > 0)
                        {
                            if ((lineResult.Last().Value.Last() + 1) == line)
                            {
                                lineResult.Last().Value.Add(line);
                                isNew = false;
                            }
                        }
                        if (isNew)
                        {
                            lineData = new List<int> { line };
                            lineResult.Add(line, lineData);
                        }
                    }

                    break;
				}
			case ERowCompareResultType.SUCCEED:
				{
					if (param.CheckCanMergeByDirtyRow() == false)
					{
						param.CurrentNotDirtyUpRowNum = 0;
						break;
					}
					int iGroupIdx = groupIdx;
					int iRowIdx = rowIdx;
					bool isDownDirty = false;
					for (int i = 0;i < param.NeedNotDirtyDownRowNum; ++i)
					{
						var nRow = param.GetNextRow(detailRoot, ref iGroupIdx, ref iRowIdx, 1);
						if (nRow != null)
						{
							var cResult = CompareRow(detailRoot, nRow, iGroupIdx, iRowIdx, ref crossJugeRowList);
							if (cResult == ERowCompareResultType.FAILED || cResult == ERowCompareResultType.FAILED_CROSSJUGE)
							{
								isDownDirty = true;
								break;
							}
						}
					}
					if (isDownDirty == true)
					{
						param.CurrentNotDirtyUpRowNum = 0;
						break;
					}
					
					param.CurrentNotDirtyUpRowNum++;
					if (drow.lines.Length >= 2)
					{
						if (drow.lines[0].file == 1 && drow.lines[1].file == 2)
						{
							//可以合并，修改
							ReplaceLine(ref param.LineList1, param.GetRealHandleLineNum(), param.GetRealLineText(drow.lines[1].file, drow.lines[1].number));
						}
						else if (drow.lines[0].file == 1 && drow.lines[1].file == 3)
						{
							//可以合并，删除
							RemoveLine(ref param.LineList1, param.GetRealHandleLineNum());
							param.OffsetLineNum--;
						}
						else if (drow.lines[0].file == 2 && drow.lines[1].file == 3)
						{
							//可以合并，新增
							InsertLine(ref param.LineList1, param.GetRealHandleLineNum(), param.GetRealLineText(drow.lines[0].file, drow.lines[0].number));
							param.OffsetLineNum++;
						}
					}
					else
					{
						if (drow.lines[0].file == 1)
						{
							//可以合并，删除
							RemoveLine(ref param.LineList1, param.GetRealHandleLineNum());
							param.OffsetLineNum--;
						}
						else if (drow.lines[0].file == 2)
						{
							//可以合并，新增
							InsertLine(ref param.LineList1, param.GetRealHandleLineNum(), param.GetRealLineText(drow.lines[0].file, drow.lines[0].number));
							param.OffsetLineNum++;
						}
						else if (drow.lines[0].file == 3)
						{
							//不用管
						}
					}
					break;
				}
			case ERowCompareResultType.SUCCEED_CROSSJUGE:
				{
					MergeRow(ERowCompareResultType.SUCCEED, drow, detailRoot, ref param, groupIdx, rowIdx, ref crossJugeRowList);
					break;
				}
			default:
				{
					param.CurrentNotDirtyUpRowNum = 0;
					break;
				}
		}
	}

	ERowCompareResultType CompareRow(DReport detailRoot, DRow drow, int groupIdx, int rowIdx, ref HashSet<DRow> crossJugeRowList)
	{
		if (crossJugeRowList.Contains(drow))
			return ERowCompareResultType.SUCCEED;

		//每一行做3路对比
		//若这一行有3路
		if (drow.lines.Length == 3)
		{
			//第1路必须要跟第3路是一样的
			if (drow.lines[0].GetText() == drow.lines[2].GetText())
			{
				//可以合并，修改
				return ERowCompareResultType.SUCCEED;
			}
			else
			{
				//不能合并
				return ERowCompareResultType.FAILED;
			}
		}
		//若这一行有2路
		else if (drow.lines.Length == 2)
		{
			//这2路如果是1、3路且第1路跟第3路是一样的，则直接合并
			if (drow.lines[0].file == 1 && drow.lines[1].file == 3)
			{
				if (drow.lines[0].GetText() == drow.lines[1].GetText())
				{
					//可以合并，删除
					return ERowCompareResultType.SUCCEED;
				}
				else
				{
					//不能合并
					return ERowCompareResultType.FAILED;
				}
			}
			else if (drow.lines[0].file == 1 && drow.lines[1].file == 2)
			{
				//开始进行交叉判断复杂情况
				bool re = CrossJuge(detailRoot, 1, groupIdx, rowIdx, groupIdx + 1, 0, ref crossJugeRowList);
				if (re)
				{
					return ERowCompareResultType.SUCCEED_CROSSJUGE;
				}
				else
				{
					//1路可能又一些空格或者Tab字符，这种情况先交叉判断，不行就直接新增或修改
					if (string.IsNullOrEmpty(drow.lines[0].GetText()))
					{
						return ERowCompareResultType.SUCCEED;
					}
					//不能合并
					return ERowCompareResultType.FAILED_CROSSJUGE;
				}
			}
			else if (drow.lines[0].file == 2 && drow.lines[1].file == 3)
			{
				//开始进行交叉判断复杂情况
				bool re = CrossJuge(detailRoot, 3, groupIdx, rowIdx, groupIdx + 1, 0, ref crossJugeRowList);
				if (re)
				{
					return ERowCompareResultType.SUCCEED_CROSSJUGE;
				}
				else
				{
					//3路可能又一些空格或者Tab字符，这种情况先交叉判断，不行就直接新增或修改
					if (string.IsNullOrEmpty(drow.lines[1].GetText()))
					{
						return ERowCompareResultType.SUCCEED;
					}
					//不能合并
					return ERowCompareResultType.FAILED_CROSSJUGE;
				}
			}
		}
		//若这一行有1路
		else if (drow.lines.Length == 1)
		{
			//如果是第2路
			if (drow.lines[0].file == 2)
			{
				//可以合并，新增
				return ERowCompareResultType.SUCCEED;
			}
			//如果是第1\3路
			else
			{
				//开始进行交叉判断复杂情况
				bool re = CrossJuge(detailRoot, drow.lines[0].file, groupIdx, rowIdx, groupIdx + 1, 0, ref crossJugeRowList);
				if (re)
				{
					return ERowCompareResultType.SUCCEED_CROSSJUGE;
				}
				else
				{
					//1、3路可能又一些空格或者Tab字符，这种情况先交叉判断，不行就直接新增或修改
					if (string.IsNullOrEmpty(drow.lines[0].GetText()))
					{
						return ERowCompareResultType.SUCCEED;
					}
					//不能合并
					return ERowCompareResultType.FAILED_CROSSJUGE;
				}
			}
		}

		//怎么可能一行都没有
		return ERowCompareResultType.FAILED;
	}

	public void CompareDetail(string xmlPath)
	{
		if (OnlyFoderCompare) return;
		if (File.Exists(xmlPath) == false) return;
		HashSet<DRow> crossJugeRowList = new HashSet<DRow>();
		//以下详细文件的合并，为了以防万一，必须满足以下的所有条件才会合并，否则跳过，人工自己合并
		DReport detailRoot = Deserialize<DReport>(xmlPath);
		if (detailRoot.type != "FileComparison") return;

		string file1 = detailRoot.metaData.fileDetails[0].location + "\\" + detailRoot.metaData.fileDetails[0].name;
		string file2 = detailRoot.metaData.fileDetails[1].location + "\\" + detailRoot.metaData.fileDetails[1].name;
		string file3 = detailRoot.metaData.fileDetails[2].location + "\\" + detailRoot.metaData.fileDetails[2].name;

		CompareDetailParam param = new CompareDetailParam();
		param.NeedNotDirtyUpRowNum = UpNeedNotDirtyRow;
		param.NeedNotDirtyDownRowNum = DownNeedNotDirtyRow ;
		GetFileLine(ref param.LineList1, file1);
		GetFileLine(ref param.LineList2, file2);
		GetFileLine(ref param.LineList3, file3);
		DebugLog("CompareDetail: FileName:" + detailRoot.metaData.fileDetails[0].name);
		int groupNum = detailRoot.rowData.rowGroups.Length;
		for (int groupIdx = 0; groupIdx < groupNum; ++groupIdx)
		{
			var dgroup = detailRoot.rowData.rowGroups[groupIdx];
			if (CompareDetailCallBack != null)
				CompareDetailCallBack(groupNum, groupIdx, "");
			for (int rowIdx = 0; rowIdx < dgroup.rows.Length; ++rowIdx)
			{
				param.RealRowIdx++;
				var drow = dgroup.rows[rowIdx];
				//如果3路里有其中一路有变化才做对比
				bool needHandl = false;
				foreach (var dline in drow.lines)
				{
					if (dline.modifyClass != "U" && dline.modifyClass != "UU")
					{
						needHandl = true;
						break;
					}
				}

				//记录最左边路上一次能获取的行数
				if (drow.lines[0].file == 1)
					param.HandleLineNum = drow.lines[0].number;

				if (needHandl == false)
				{
					param.CurrentNotDirtyUpRowNum++;
					continue;
				}

				var result = CompareRow(detailRoot, drow, groupIdx, rowIdx, ref crossJugeRowList);
				MergeRow(result, drow, detailRoot, ref param, groupIdx, rowIdx, ref crossJugeRowList);
			}
		}

		SaveFileLine(ref param.LineList1, file1);
	}

	public void Compare(string xmlPath)
	{
		ClearLogAndResult();
		if (File.Exists(xmlPath) == false) return;

		xmlPath = xmlPath.Replace('\\', '/');
		string xmlRootPath = xmlPath.Substring(0, xmlPath.LastIndexOf('/') + 1);
		MGReport root = Deserialize<MGReport>(xmlPath);
		if (root.type == "FileComparison")
		{
			CompareDetail(xmlPath);
			return;
		}

		if (root.type != "FolderComparison") return;

		List<string> needHandleFile = new List<string>();
		List<string> currentFilePath = new List<string>();
		List<string> rootPath = new List<string>();
		int rowCount = root.rowData.rows.Length;
		for (int rowidx = 0; rowidx < rowCount; ++rowidx)
		{
			var row = root.rowData.rows[rowidx];
			if (row.files.Length <= 0) continue;

			//左边路文件路径
			string file1 = root.metaData.folderDetails[0].location + "\\" + root.metaData.folderDetails[0].name + "\\";
			//中间路文件路径
			string file2 = root.metaData.folderDetails[1].location + "\\" + root.metaData.folderDetails[1].name + "\\";
			//右边路文件路径
			string file3 = root.metaData.folderDetails[2].location + "\\" + root.metaData.folderDetails[2].name + "\\";

			//这一行文件的路径
			int count = currentFilePath.Count;
			for (int i = row.files[0].level; i < count; ++i)
			{
				currentFilePath.RemoveAt(currentFilePath.Count - 1);
			}
			currentFilePath.Add(row.files[0].name);

			if (IsExceptFolder(currentFilePath) == true) continue;
			if (IsSpecifyFolder(currentFilePath) == false) continue;
			if (IsExceptExtension(currentFilePath) == true) continue;
			if (IsSpecifyExtension(currentFilePath) == false) continue;


			currentFilePath.ForEach(path => { file1 = file1 + "/" + path; file2 = file2 + "/" + path; file3 = file3 + "/" + path; });

			if (CompareCallBack != null)
				CompareCallBack(rowCount, rowidx, file1);

			//先做默认3路处理，以后输出给脚本文件根据各项目需要做更灵活的修改
			//3路都有文件的话
			if (row.files.Length == 3)
			{
				//2路有更改（目标版本资源）并且3路（上一个存进版本资源）没更改，说明我们完全没改动过这个文件，可以直接用2路覆盖1路
				if (row.files[1].ChangeCount > 0 && row.files[2].ChangeCount == 0)
				{
					ReplaceFile(file2, file1);
				}
				//2路有更改（目标版本资源）并且3路（上一个存进版本资源）也有更改，那就只能打开着俩文件的xml进一步分析合并了
				else if (row.files[1].ChangeCount > 0)
				{
					CompareDetail(xmlRootPath + row.url);
				}
			}
			//3路里只有两路有文件的话
			else if (row.files.Length == 2)
			{
				//1、3路有文件说明可以删
				if (row.files[0].folder == 1 && row.files[1].folder == 3)
				{
					RemoveFile(file1);
				}
			}
			//3路里只有一路有文件的话
			else if (row.files.Length == 1)
			{
				//2路有文件说明是新增的
				if (row.files[0].folder == 2)
				{
					InsertFile(file2, file1);
				}
			}
		}

        SaveResult();
    }

	void DebugLog(string log)
	{
#if UNITY_EDITOR
		UnityEngine.Debug.Log(log);
#endif
		StreamWriter writer = null;
		using (writer = new StreamWriter(LogPath, true))
		{
			writer.WriteLine(log);
			writer.Flush();
			writer.Close();
		}
	}

	void ClearLogAndResult()
	{
		StreamWriter writer = null;
		using (writer = new StreamWriter(LogPath))
		{
			writer.WriteLine("");
			writer.Flush();
			writer.Close();
		}

        ReplaceFileCount = 0;
        InsertFileCount = 0;
        RemoveFileCount = 0;
        ReplaceLineCount = 0;
        InsertLineCount = 0;
        RemoveLineCount = 0;
        FaildFileResult = new Dictionary<string, Dictionary<int, List<int>>>();
}

	bool IsExceptFolder(List<string> currentFilePath)
	{
		if (string.IsNullOrEmpty(ExceptFolder) == true) return false;
		string currentPath = "";
		currentFilePath.ForEach(path => currentPath += (path + '/'));
		currentPath = currentPath.Trim('/');
		var foldersList = ExceptFolder.Trim(';').Split(';');
		for (int i = 0; i < foldersList.Length; ++i)
		{
			var folders = foldersList[i].Trim('/');
			if (currentPath.IndexOf(folders) >= 0)
				return true;
		}
		return false;
	}

	bool IsSpecifyFolder(List<string> currentFilePath)
	{
		if (string.IsNullOrEmpty(SpecifyFolder) == true) return true;
		string currentPath = "";
		currentFilePath.ForEach(path => currentPath += (path + '/'));
		currentPath = currentPath.Trim('/');
		if (currentFilePath.Count <= 0) return true;
		var foldersList = SpecifyFolder.Trim(';').Split(';');
		for (int i = 0; i < foldersList.Length; ++i)
		{
			var folders = foldersList[i].Trim('/');
			if (currentPath.IndexOf(folders) >= 0)
				return true;
			if (folders.IndexOf(currentPath) >= 0)
				return true;
		}
		return false;
	}

	bool IsExceptExtension(List<string> currentFilePath)
	{
		if (string.IsNullOrEmpty(ExceptExtension) == true) return false;
		var extensions = ExceptExtension.Trim(';').Split(';');
		if (currentFilePath.Count <= 0) return false;
		var file = currentFilePath[currentFilePath.Count - 1];
		if (file.LastIndexOf(".") < 0) return false;
		string fileext = file.Substring(file.LastIndexOf(".") + 1, (file.Length - file.LastIndexOf(".") - 1));
		for (int i = 0; i < extensions.Length; ++i)
		{
			if (extensions[i] == fileext)
				return true;
		}
		return false;
	}

	bool IsSpecifyExtension(List<string> currentFilePath)
	{
		if (string.IsNullOrEmpty(SpecifyExtension) == true) return true;
		var extensions = SpecifyExtension.Trim(';').Split(';');
		if (currentFilePath.Count <= 0) return true;
		var file = currentFilePath[currentFilePath.Count - 1];
		if (file.LastIndexOf(".") < 0) return true;
		string fileext = file.Substring(file.LastIndexOf(".") + 1, (file.Length - file.LastIndexOf(".") - 1));
		for (int i = 0; i < extensions.Length; ++i)
		{
			if (extensions[i] == fileext)
				return true;
		}
		return false;
	}

	T Serialize<T>(string filename, T data)
	{
		using (var stream = new FileStream(filename, FileMode.Create))
		{

			var serializer = new XmlSerializer(typeof(T));
			var streamWriter = new StreamWriter(stream, System.Text.Encoding.UTF8);
			serializer.Serialize(streamWriter, data);
		}

		return data;
	}

	T Deserialize<T>(string filename)
	{
		using (var stream = new FileStream(filename, FileMode.Open))
		{
			var serializer = new XmlSerializer(typeof(T));
			return (T)serializer.Deserialize(stream);
		}
	}
}

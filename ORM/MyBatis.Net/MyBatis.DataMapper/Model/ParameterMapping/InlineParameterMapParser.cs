#region Apache Notice
/*****************************************************************************
 * $Header: $
 * $Revision: 408099 $
 * $Date: 2008-10-11 10:07:44 -0600 (Sat, 11 Oct 2008) $
 * 
 * iBATIS.NET Data Mapper
 * Copyright (C) 2008/2005 - The Apache Software Foundation
 *  
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 ********************************************************************************/
#endregion

#region Using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyBatis.DataMapper.DataExchange;
using MyBatis.DataMapper.Exceptions;
using MyBatis.DataMapper.Model.Sql.Dynamic;
using MyBatis.DataMapper.Model.Statements;
using MyBatis.Common.Exceptions;
using MyBatis.Common.Utilities;

#endregion 

namespace MyBatis.DataMapper.Model.ParameterMapping
{
	/// <summary>
	/// Builds Paremeter property for Inline Parameter Map.
	/// </summary>
	public sealed class InlineParameterMapParser
	{
		private const string PARAMETER_TOKEN = "#";
		private const string PARAM_DELIM = ":";
        private const string MARK_TOKEN = "?";

        private const string NEW_BEGIN_TOKEN = "@{";
        private const string NEW_END_TOKEN = "}";

        /// <summary>
        /// Parse Inline ParameterMap
        /// ��statement insert update delete select ���Ĳ������з���
        /// </summary>
        /// <param name="dataExchangeFactory">The data exchange factory.</param>
        /// <param name="statementId">The statement id.</param>
        /// <param name="statement">The statement.</param>
        /// <param name="sqlStatement">The SQL statement.</param>
        /// <returns>A new sql command text.</returns>
        public static SqlText ParseInlineParameterMap(DataExchangeFactory dataExchangeFactory, string statementId, IStatement statement, string sqlStatement)
		{
			string newSql = sqlStatement;//��ֵ�õ�һ��SQL�������
            List<ParameterProperty> mappingList = new List<ParameterProperty>();
			Type parameterClassType = null;

			if (statement != null)
			{
				parameterClassType = statement.ParameterClass;//��ǰ�ڵ������еĲ����� �᲻���ж������������������
			}

            if (sqlStatement.Contains(NEW_BEGIN_TOKEN))//���SQL������"@{"
            {
                // V3 parameter syntax
                //@{propertyName,column=string,type=string,dbype=string,direction=[Input/Output/InputOutput],nullValue=string,handler=string}

                /*��@{��ͷ������
                    <procedure id="InsertAccountViaSPWithDynamicParameter" parameterClass="Account" >
                        ps_InsertAccountWithDefault
                        @{Id,column=Account_ID}//ÿһ��@��Ӧһ��ParameterProperty��
                        ,@{FirstName,column=Account_FirstName}
                        ,@{LastName,column=Account_LastName}
                         ,@{EmailAddress,column=Account_Email,nullValue=no_email@provided.com}
                         <isNotNull property="NullBannerOption">
                                ,@{NullBannerOption,column=Account_Banner_Option,dbType=Varchar,type=bool}
                        </isNotNull>
                        @{CartOption,column=Account_Cart_Option,handler=HundredsBool}
                   </procedure>
                           */
                if (newSql != null)
                {
                    string toAnalyse = newSql;
                    int start = toAnalyse.IndexOf(NEW_BEGIN_TOKEN);//@{���±�
                    int end = toAnalyse.IndexOf(NEW_END_TOKEN);//"}"���±�
                    StringBuilder newSqlBuffer = new StringBuilder();

                    while (start > -1 && end > start)
                    {
                        //������� @{ ******  }��ֳ�3���ַ���
                        string prepend = toAnalyse.Substring(0, start);//@{����
                        string append = toAnalyse.Substring(end + NEW_END_TOKEN.Length);//}.....����
                       
                        //EmailAddress,column=string,type=string,dbType=Varchar,nullValue=no_email@provided.com
                        string parameter = toAnalyse.Substring(start + NEW_BEGIN_TOKEN.Length, end - start - NEW_BEGIN_TOKEN.Length);//��һ���м����ݲ���

                        ParameterProperty mapping = NewParseMapping(parameter, parameterClassType, dataExchangeFactory, statementId);
                        mappingList.Add(mapping);//��������õ���ParameterProperty�����б���
                        newSqlBuffer.Append(prepend);//����@{"���� ������ "},@{"���ּ���
                        newSqlBuffer.Append(MARK_TOKEN);//���� "?"��־

                        //�����ж������ ����ѭ������׼��
                        toAnalyse = append;
                        start = toAnalyse.IndexOf(NEW_BEGIN_TOKEN);
                        end = toAnalyse.IndexOf(NEW_END_TOKEN);
                    }
                    /*while ѭ����ɺ��������
                     *  ps_InsertAccountWithDefault
                     *  @{��}��@{��}�Ĵ�Ÿ�ʽ ���浽newSql��
                                   */
                    newSqlBuffer.Append(toAnalyse);
                    newSql = newSqlBuffer.ToString();
                }
            }
            else
            {
                #region old syntax
                /*
                   <insert id="InsertAccountViaInlineParameters"  parameterClass="Account" >
                       insert into Accounts
                      (Account_ID, Account_FirstName, Account_LastName, Account_Email)
                      values
                    (#Id#, #FirstName#, #LastName#, #EmailAddress:VarChar:no_email@provided.com#)
                   </insert>
                */
                //ÿ# #֮�����һ��ParameterProperty��
                StringTokenizer parser = new StringTokenizer(sqlStatement, PARAMETER_TOKEN, true);//"#"���� true��ʾ �����#�򷵻�
			    StringBuilder newSqlBuffer = new StringBuilder();

			    string token = null;
			    string lastToken = null;

			    IEnumerator enumerator = parser.GetEnumerator();

			    while (enumerator.MoveNext())//��ȡ��ǰ���ŵ���һ����֮����ַ��� 
			    {
                    token = (string)enumerator.Current;//Current���������˻�ȡ��ǰ���ŵ���һ����֮����ַ��� �� #

				    if (PARAMETER_TOKEN.Equals(lastToken)) //�����һ����#
				    {
                        // Double token ## = # 
                        //�ȴ���#
					    if (PARAMETER_TOKEN.Equals(token)) //�����ǰҲ��#
					    {
						    newSqlBuffer.Append(PARAMETER_TOKEN);
						    token = null;
					    }
                        else //�����ǰ����# Ҳ��������#֮����ַ���
					    {
						    ParameterProperty mapping = null; 
                            //�ж�# #֮��������Ƿ��С�����
						    if (token.IndexOf(PARAM_DELIM) > -1) //����ַ����к��С�:"���� �����ú���
						    {
                                mapping = OldParseMapping(token, parameterClassType, dataExchangeFactory);
						    } 
						    else //������"��",���ٴ�Ĭ��","Ϊ�ָ���
						    {
                                mapping = NewParseMapping(token, parameterClassType, dataExchangeFactory, statementId);
						    }															 

						    mappingList.Add(mapping);
                            newSqlBuffer.Append(MARK_TOKEN + " ");//���"?"��Ϊ��־����

						    enumerator.MoveNext();
						    token = (string)enumerator.Current;
						    if (!PARAMETER_TOKEN.Equals(token)) 
						    {
							    throw new DataMapperException("Unterminated inline parameter in mapped statement (" + statementId + ").");
						    }
						    token = null;
					    }
				    } 
				    else 
				    {
					    if (!PARAMETER_TOKEN.Equals(token)) 
					    {
						    newSqlBuffer.Append(token);
					    }
				    }

				    lastToken = token;
			    }
                /*������sql����ʽӦ��Ϊ
                          insert into Accounts
                        (Account_ID, Account_FirstName, Account_LastName, Account_Email)
                        values
                       (?, ?, ?, ?)
                 *����Ӧ�Ĳ��������� mappingList��  
                 *���������� �ŵ�SqlText����
                            */
                newSql = newSqlBuffer.ToString();
 
	            #endregion            
            }
            //��List����ת��Ϊ�������ʽ
			ParameterProperty[] mappingArray =  mappingList.ToArray();                

            //�������ļ�SQL��� �� �����б� ���浽 SqlText��
			SqlText sqlText = new SqlText();
			sqlText.Text = newSql;
			sqlText.Parameters = mappingArray;

			return sqlText;
		}

        /// <summary>
        /// Parse inline parameter with syntax as
        /// #propertyName,type=string,dbype=Varchar,direction=Input,nullValue=N/A,handler=string#
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="parameterClassType">Type of the parameter class.</param>
        /// <param name="dataExchangeFactory">The data exchange factory.</param>
        /// <param name="statementId">The statement id.</param>
        /// <returns></returns>
        /// <example>
        /// #propertyName,type=string,dbype=Varchar,direction=Input,nullValue=N/A,handler=string#
        /// </example>
        private static ParameterProperty NewParseMapping(string token, Type parameterClassType, DataExchangeFactory dataExchangeFactory, string statementId) 
		{
            string propertyName = string.Empty;
            string type = string.Empty;
            string dbType = string.Empty;
            string direction = string.Empty;
            string callBack = string.Empty;
            string nullValue = null;
            string columnName = string.Empty;

			StringTokenizer paramParser = new StringTokenizer(token, "=,", false);
            //���ַ�propertyName,type=string,dbype=Varchar,direction=Input,nullValue=N/A,handler=stringΪ��
			IEnumerator enumeratorParam = paramParser.GetEnumerator();
            enumeratorParam.MoveNext();//�˴���ȡ propertyName���浽�ڲ���next��

            propertyName = ((string)enumeratorParam.Current).Trim();//ʵ���ϵ�����next������

			while (enumeratorParam.MoveNext()) 
			{
                string field = ((string)enumeratorParam.Current).Trim().ToLower();
                //ÿMoveNextһ�� ���һ�����ݵ�Current��

				if (enumeratorParam.MoveNext()) 
				{
                    string value = ((string)enumeratorParam.Current).Trim();
					if ("type".Equals(field)) 
					{
                        type = value;
					} 
					else if ("dbtype".Equals(field)) 
					{
                        dbType = value;
					} 
					else if ("direction".Equals(field)) 
					{
                        direction = value;
					}
                    else if ("nullvalue".Equals(field)) 
					{
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            nullValue = value.Substring(1, value.Length-2);
                        }
                        else
                        {
                            nullValue = value;
                        }
					} 
					else if ("handler".Equals(field)) 
					{
                        callBack = value;
					}
                    else if ("column".Equals(field))
                    {
                        columnName = value;
                    } 
					else 
					{
						throw new DataMapperException("When parsing inline parameter for statement '"+statementId+"', can't recognize parameter mapping field: '" + field + "' in " + token+", check your inline parameter syntax.");
					}
				} 
				else 
				{
					throw new DataMapperException("Incorrect inline parameter map format (missmatched name=value pairs): " + token);
				}
			}

            //����������ַ����ݴ��뵽ParameterProperty����
            return new ParameterProperty(
                propertyName,
                columnName,
                callBack,
                type,
                dbType,
                direction,
                nullValue,
                0,
                0,
                -1,
                parameterClassType,
                dataExchangeFactory);
		}

        /// <summary>
        /// Parse inline parameter with syntax as
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="parameterClassType">Type of the parameter class.</param>
        /// <param name="dataExchangeFactory">The data exchange factory.</param>
        /// <example>
        /// #propertyName:dbType:nullValue#
        /// </example>
        /// <returns></returns>
        private static ParameterProperty OldParseMapping(string token, Type parameterClassType, DataExchangeFactory dataExchangeFactory) 
		{
            //Ŀ����ǽ�������3������
            string propertyName = string.Empty;
            string dbType = string.Empty;
            string nullValue = null;

			if (token.IndexOf(PARAM_DELIM) > -1) 
			{
				StringTokenizer paramParser = new StringTokenizer(token, PARAM_DELIM, true);
				IEnumerator enumeratorParam = paramParser.GetEnumerator();

				int n1 = paramParser.TokenNumber;
				if (n1 == 3) 
				{
					enumeratorParam.MoveNext();
					propertyName = ((string)enumeratorParam.Current).Trim();

					enumeratorParam.MoveNext();
					enumeratorParam.MoveNext(); //ignore ":"
                    dbType = ((string)enumeratorParam.Current).Trim();
				} 
				else if (n1 >= 5) 
				{
					enumeratorParam.MoveNext();
					propertyName = ((string)enumeratorParam.Current).Trim();

					enumeratorParam.MoveNext();
					enumeratorParam.MoveNext(); //ignore ":"
                    dbType = ((string)enumeratorParam.Current).Trim();

					enumeratorParam.MoveNext();
					enumeratorParam.MoveNext(); //ignore ":"
					nullValue = ((string)enumeratorParam.Current).Trim();

					while (enumeratorParam.MoveNext()) 
					{
						nullValue = nullValue + ((string)enumeratorParam.Current).Trim();
					}
				} 
				else 
				{
					throw new ConfigurationException("Incorrect inline parameter map format: " + token);
				}
			} 
			else 
			{
				propertyName = token;
			}

            return new ParameterProperty(
                propertyName,
                string.Empty,
                string.Empty,
                string.Empty,
                dbType,
                string.Empty,
                nullValue,
                0,
                0,
                -1,
                parameterClassType,
                dataExchangeFactory);
		}

	}
}

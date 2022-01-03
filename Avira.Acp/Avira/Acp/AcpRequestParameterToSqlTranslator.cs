using System;
using System.Collections.Generic;

namespace Avira.Acp
{
    public class AcpRequestParameterToSqlTranslator
    {
        public string WherePredicate { get; private set; }

        public string LimitPredicate { get; private set; }

        public AcpRequestParameterToSqlTranslator(List<AcpRequestParameter> parameters,
            List<AcpToSqlParameterTranslator> acpToSqlFieldTranslators)
        {
            ProcessParameters(parameters, acpToSqlFieldTranslators);
        }

        private void ProcessParameters(List<AcpRequestParameter> parameters,
            List<AcpToSqlParameterTranslator> acpToSqlFieldTranslators)
        {
            string text = string.Empty;
            string limitPredicate = string.Empty;
            foreach (AcpRequestParameter acpRequestParameter in parameters)
            {
                if (acpRequestParameter.Type == AcpRequestParameter.ParameterType.Filter)
                {
                    AcpToSqlParameterTranslator acpToSqlParameterTranslator =
                        acpToSqlFieldTranslators.Find((AcpToSqlParameterTranslator t) =>
                            t.AcpFieldName == acpRequestParameter.Property);
                    if (acpToSqlParameterTranslator == null)
                    {
                        throw new Exception("Unknown attribute");
                    }

                    if (text.Length > 0)
                    {
                        text += " AND ";
                    }

                    text = text + acpToSqlParameterTranslator.SqlFieldName + " " + GetFilterOperation(
                        acpToSqlParameterTranslator.Converter(acpRequestParameter.Value), acpRequestParameter.Operator);
                }
                else if (acpRequestParameter.Type == AcpRequestParameter.ParameterType.Page &&
                         acpRequestParameter.Property == "size")
                {
                    limitPredicate = AcpToSqlParameterTranslator.GetVerifiedNumber(acpRequestParameter.Value);
                }
            }

            WherePredicate = text.TrimEnd();
            LimitPredicate = limitPredicate;
        }

        private string GetFilterOperation(string parameterValue, AcpRequestParameter.OperatorType @operator)
        {
            if (@operator == AcpRequestParameter.OperatorType.Equals && parameterValue.Contains(","))
            {
                return "IN (" + parameterValue + ")";
            }

            return AcpRequestParameter.GetOperatorString(@operator) + " " + parameterValue;
        }
    }
}
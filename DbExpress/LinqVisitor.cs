using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Data;
using System.Reflection;

namespace AutoORMCore.DbExpress
{
    internal class LinqVisitor:IDisposable
    {
        List<object> queue;
        public LinqVisitor()
        {
            queue = new List<object>();
        }
        internal object Visit(Expression exp)
        {
            if (exp is LambdaExpression) { VisitLambda(exp as LambdaExpression); return queue; }
            else if (exp is MethodCallExpression) { return VisitMethodCall(exp as MethodCallExpression); }
            else if (exp is ConstantExpression) { return VisitConstant(exp as ConstantExpression); }
            else if (exp is UnaryExpression) { return Visit((exp as UnaryExpression).Operand); }
            else if (exp is MemberExpression)
            {
                var tp = exp as MemberExpression;
                if (tp.Member is FieldInfo)
                {
                    return (tp.Member as FieldInfo).GetValue(Visit(tp.Expression));
                }
                else if (tp.Member is PropertyInfo)
                {
                    return (tp.Member as PropertyInfo).GetValue(Visit(tp.Expression),null);
                    //4.5 return (tp.Member as PropertyInfo).GetValue(Visit(tp.Expression));
                }
            }
            else if (exp is BinaryExpression) { return VisitBinary(exp as BinaryExpression); }
            return null;
        }
        internal object VisitLambda(LambdaExpression exp) { return Visit(exp.Body); }
        internal object VisitMethodCall(MethodCallExpression exp) {
            List<object> lt = new List<object>();
            foreach (var n in exp.Arguments)
            {
                if (n is Expression) { lt.Add(Visit(n as Expression)); continue; }
                lt.Add(n);
            }
            return exp.Method.Invoke(Visit(exp.Object), lt.ToArray()); }
        internal object VisitConstant(ConstantExpression exp) { return exp.Value; }
        internal IEnumerable<string> VisitNew(NewExpression node)
        {
            List<string> lt = new List<string>();
            foreach (var n in node.Members)
            {
                lt.Add(n.Name);
            }
            return lt;
        }
        internal object VisitBinary(BinaryExpression exp)
        {
            if (exp.Left is MemberExpression)
            {
                var tp = exp.Left as MemberExpression;
                if (tp.Expression is ParameterExpression)
                {
                    queue.Add(tp.Member.Name);
                    queue.Add(GetExp(exp.NodeType));
                    queue.Add(Visit(exp.Right));
                    return string.Empty;
                }
            }
            else if (exp is BinaryExpression)
            {
                Visit(exp.Left);
                queue.Add(GetExp(exp.NodeType));
                Visit(exp.Right);
                return string.Empty;
            }
            var tp1=Visit(exp.Left);
            if (tp1 is Array)
            {
               return (tp1 as Array).GetValue((int)Visit(exp.Right));
            }
            return string.Empty;
        }
        internal static string GetExp(ExpressionType e)
        {
            switch (e)
            {
                case ExpressionType.And: { return " AND "; }
                case ExpressionType.AndAlso: { return " AND "; }
                case ExpressionType.Or: { return " OR "; }
                case ExpressionType.OrElse: { return " OR "; }
                case ExpressionType.LessThan: { return "<"; }
                case ExpressionType.GreaterThan: { return ">"; }
                case ExpressionType.Equal: { return "="; }
                case ExpressionType.GreaterThanOrEqual: { return ">="; }
                case ExpressionType.LessThanOrEqual: { return "<="; }
                case ExpressionType.Not: { return "<>"; }
            }
            return string.Empty;
        }
        public void Dispose() { queue.Clear(); queue = null; }
    }
}

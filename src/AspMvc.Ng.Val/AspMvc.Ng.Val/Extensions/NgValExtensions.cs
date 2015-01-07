using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;
using Newtonsoft.Json;

namespace AspMvc.Ng.Val.Extensions
{
    public static class NgValExtensions
    {
        public static MvcHtmlString NgValFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            return NgValFor(htmlHelper, expression, null, new RouteValueDictionary());
        }

        public static MvcHtmlString NgValFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string validationMessage)
        {
            return NgValFor(htmlHelper, expression, validationMessage, new RouteValueDictionary());
        }

        public static MvcHtmlString NgValFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string validationMessage, object htmlAttributes)
        {
            return NgValFor(htmlHelper, expression, validationMessage, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static MvcHtmlString NgValFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string validationMessage, IDictionary<string, object> htmlAttributes)
        {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>());
            var name = ExpressionHelper.GetExpressionText(expression);
            var validations = ModelValidatorProviders.Providers.GetValidators(
                    metadata ?? ModelMetadata.FromStringExpression(name, new ViewDataDictionary()),
                    new ControllerContext())
                    .SelectMany(v => v.GetClientValidationRules()).ToArray();

            List<ValidatorMessage> validatorMessages = new List<ValidatorMessage>();

            foreach (var validation in validations)
            {

                var dictionaryType = DictionaryValidationType.Where(x => x.Key == validation.ValidationType).ToArray();

                if (dictionaryType.Any())
                {
                    validatorMessages.AddRange(dictionaryType.Select(type => new ValidatorMessage()
                    {
                        Type = type.Value, 
                        Message = validation.ErrorMessage.Replace("'","")
                    }));
                }
                else
                {
                    ValidatorMessage validatorMessage = new ValidatorMessage()
                    {
                        Type = validation.ValidationType,
                        Message = validation.ErrorMessage.Replace("'", "")
                    };

                    validatorMessages.Add(validatorMessage);
                }
               
            }

            string result = "";

            result += GetNgValDirectiveString(validatorMessages);
            result += GetValidatorDirectivesString(validations);

            return new MvcHtmlString(result);
        }

        private static string GetValidatorDirectivesString(IEnumerable<ModelClientValidationRule> validations)
        {
            return validations.Aggregate("", (current, val) => current + (" " + ConvertMvcClientValidatorToAngularValidatorString(val)));
        }

        private static string ConvertMvcClientValidatorToAngularValidatorString(ModelClientValidationRule val)
        {
            switch (val.ValidationType.ToLower())
            {
                case "required":
                    return "required";
                case "range":
                    return string.Format("min=\"{0}\" max=\"{1}\"", val.ValidationParameters["min"], val.ValidationParameters["max"]);
                case "regex":
                    return string.Format("ng-pattern=\"/{0}/\"", val.ValidationParameters["pattern"]);
                case "length":
                    string lengthRes = "";
                    if (val.ValidationParameters.ContainsKey("min"))
                        lengthRes += string.Format("ng-minlength=\"{0}\" ", val.ValidationParameters["min"]);
                    if (val.ValidationParameters.ContainsKey("max"))
                        lengthRes += string.Format("ng-maxlength=\"{0}\"", val.ValidationParameters["max"]);
                    return lengthRes.TrimEnd();
                default:
                    return string.Format("{0}=\"{1}\"", val.ValidationType, JsonConvert.SerializeObject(val.ValidationParameters));
            }
        }

        private static string GetNgValDirectiveString(IEnumerable<ValidatorMessage> validatorMessages)
        {
            return string.Format("ngval-field='{0}'", JsonConvert.SerializeObject(validatorMessages));
        }

        /// <summary>
        /// Need, when different names in JS and ASP .Net
        /// </summary>
        private static readonly List<DictionaryType> DictionaryValidationType = new List<DictionaryType>()
        {
            new DictionaryType(){Key = "regex", Value = "pattern"},
            new DictionaryType(){Key = "range", Value = "min"},
            new DictionaryType(){Key = "range", Value = "max"}
        };
    }

    public class ValidatorMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    class DictionaryType
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }



}
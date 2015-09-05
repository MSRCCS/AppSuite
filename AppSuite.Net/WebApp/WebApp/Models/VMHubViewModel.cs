using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebDemo.Models
{
    public class GatewaysViewModel
    {
        public string SelectedGateway { get; set; }
        public IEnumerable<SelectListItem> Gateways { get; set; }

        public string SelectedClassifier { get; set; }
        public IEnumerable<SelectListItem> Classifiers { get; set; }
    }

}
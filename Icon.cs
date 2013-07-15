using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace makiArcGISStyle
{
  ////example
  //{
  //      "name": "Circle stroked",
  //      "tags": [
  //          "circle",
  //          "disc",
  //          "shape",
  //          "shapes",
  //          "geometric",
  //          "stroke",
  //          "round"
  //      ],
  //      "icon": "circle-stroked"
  //  }
  [DataContract]
  class Icon: IComparable<Icon>
  {
    [DataMember]
    public string name { get; set; }

    [DataMember]
    public string[] tags { get; set; }

    [DataMember]
    public string icon { get; set; }

    #region IComparable<Icon> Members

    int IComparable<Icon>.CompareTo(Icon other)
    {
      return this.name.CompareTo(other.name);
    }

    #endregion
  }
}

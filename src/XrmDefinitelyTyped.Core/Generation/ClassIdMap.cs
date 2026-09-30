namespace XrmDefinitelyTyped.Core.Generation;

internal static class ClassIdMap
{
    internal const string WebResourceClassId = "WEBRESOURCE";
    private static readonly IReadOnlyDictionary<string, ControlTypeInfo> Map =
        new Dictionary<string, ControlTypeInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["B0C6723A-8503-4FD7-BB28-C8A06AC933C2"] = new("XDTForm.BooleanControl", "Xrm.Attributes.BooleanAttribute"),
            ["5B773807-9FB2-42DB-97C3-7A91EFF8ADFF"] = new("Xrm.Controls.DateControl", "Xrm.Attributes.DateAttribute"),
            ["C3EFE0C3-0EC6-42BE-8349-CBD9079DFD8E"] = new("Xrm.Controls.NumberControl", "Xrm.Attributes.NumberAttribute"),
            ["AA987274-CE4E-4271-A803-66164311A958"] = new("Xrm.Controls.NumberControl", "Xrm.Attributes.NumberAttribute"),
            ["ADA2203E-B4CD-49BE-9DDF-234642B43B52"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["6F3FB987-393B-4D2D-859F-9D0F0349B6AD"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["0D2C745A-E5A8-4C8F-BA63-C6D3BB604660"] = new("Xrm.Controls.NumberControl", "Xrm.Attributes.NumberAttribute"),
            ["FD2A7985-3187-444E-908D-6624B21F69C0"] = new("Xrm.Controls.IframeControl", null),
            ["C6D124CA-7EDA-4A60-AEA9-7FB8D318B68F"] = new("Xrm.Controls.NumberControl", "Xrm.Attributes.NumberAttribute"),
            ["671A9387-CA5A-4D1E-8AB7-06E39DDCF6B5"] = new("Xrm.Controls.NumberControl", "Xrm.Attributes.NumberAttribute"),
            ["270BD3DB-D9AF-4782-9025-509E298DEC0A"] = new("XDTForm.LookupControl<string>", "XDTForm.LookupAttribute<string>"),
            ["533B9E00-756B-4312-95A0-DC888637AC78"] = new("Xrm.Controls.NumberControl", "Xrm.Attributes.NumberAttribute"),
            ["06375649-C143-495E-A496-C962E5B4488E"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["CBFB742C-14E7-4A17-96BB-1A13F7F64AA2"] = new("XDTForm.LookupControl<string>", "XDTForm.LookupAttribute<string>"),
            ["3EF39988-22BB-4F0B-BBBE-64B5A3748AEE"] = new("XDTForm.OptionSetControl<number>", "Xrm.Attributes.OptionSetAttribute<number>"),
            ["67FAC785-CD58-4F9F-ABB3-4B7DDC6ED5ED"] = new("XDTForm.BooleanControl", "Xrm.Attributes.BooleanAttribute"),
            ["F3015350-44A2-4AA0-97B5-00166532B5E9"] = new("XDTForm.LookupControl<string>", "XDTForm.LookupAttribute<string>"),
            ["5D68B988-0661-4DB2-BC3E-17598AD3BE6C"] = new("XDTForm.OptionSetControl<number>", "Xrm.Attributes.OptionSetAttribute<number>"),
            ["E0DECE4B-6FC8-4A8F-A065-082708572369"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["4273EDBD-AC1D-40D3-9FB2-095C621B552D"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["1E1FC551-F7A8-43AF-AC34-A8DC35C7B6D4"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["7C624A0B-F59E-493D-9583-638D34759266"] = new("XDTForm.OptionSetControl<number>", "Xrm.Attributes.OptionSetAttribute<number>"),
            ["71716B6C-711E-476C-8AB8-5D11542BFB47"] = new("Xrm.Controls.StringControl", "Xrm.Attributes.Attribute<string>"),
            ["5C5600E0-1D6E-4205-A272-BE80DA87FD42"] = new("Xrm.Controls.Control", null),
            ["62B0DF79-0464-470F-8AF7-4483CFEA0C7D"] = new("Xrm.Controls.Control", null),
            ["E7A81278-8635-4D9E-8D4D-59480B391C5B"] = new("XDTForm.SubGridControl<string>", null),
            ["9C5CA0A1-AB4D-4781-BE7E-8DFBE867B87E"] = new("Xrm.Controls.Control", null),
            ["4AA28AB7-9C13-4F57-A73D-AD894D048B5F"] = new("XDTForm.MultiSelectOptionSetControl<number>", "Xrm.Attributes.MultiSelectOptionSetAttribute<number>"),
            ["E616A57F-20E0-4534-8662-A101B5DDF4E0"] = new("Xrm.Controls.KbSearchControl", null),
            [WebResourceClassId] = new("XDTForm.WebResourceControl", null),
        };

    private static readonly ControlTypeInfo Fallback = new("Xrm.Controls.Control", "Xrm.Attributes.Attribute");

    public static ControlTypeInfo Resolve(string classId) =>
        Map.TryGetValue(classId.Trim('{', '}'), out var info) ? info : Fallback;
}

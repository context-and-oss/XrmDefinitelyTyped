Entity and form intersection
============================

Got client-side logic that needs to run on multiple forms which have similar attributes, control, tabs, etc.,
that you need to use in your code?

Make a declaration file using form intersection!

How it works
------------

Entity intersection names a shared contract, such as `ICustomer` for accounts and contacts or
`IActivity` for phone calls, emails and tasks. It matches fetched supported forms by name and form type across every named entity,
then emits their common attributes, controls, tabs and sections as shared form interfaces.
Unmatched forms and matched forms with no compatible common controls or tabs are omitted.
Entities without forms still participate in the shared XrmQuery contract.
The web generator also emits shared XrmQuery interfaces from compatible common entity attributes.

This makes it possible to create client-side code that can be used and shared safely across multiple forms.

<center><img src="img/form-intersection.png" /></center><br />

Define intersections using entity logical names with `--intersect` (`-i`):

```bash
xdt -o typings --intersect "ICustomer:account;contact, IActivity:phonecall;email;task"
```

Include these entities in your metadata selection. All ordinary forms and entity types are still
emitted. Shared attributes must have compatible types; permissions must be supported by every
member entity. An intersection defines interfaces, not a new Dataverse table or entity set.
Query the real entity endpoints using the shared XrmQuery select/filter/result contracts.

Intersection mappings take entity logical names, not form GUIDs.

Shared form interfaces can be found at `Form.<INTERFACE-NAME>.<FORM-TYPE>.<FORM-NAME>`,
for example `Form.ICustomer.Main.Information`.

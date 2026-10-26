declare namespace Xrm {
  /**
   * Enum which corresponds to the values of Xrm.Page.ui.getFormType()
   */
  const enum FormType {
    Undefined = 0,
    Create = 1,
    Update = 2,
    ReadOnly = 3,
    Disabled = 4,
    QuickCreate = 5,
    BulkEdit = 6,
  }

  namespace Collection {
    interface MatchingDelegate<T> {
      (item: T, index?: number): boolean;
    }
  }

  /**
   * Interface for an option set value.
   */
  interface Option<T> {
    /**
     * Label for the option.
     */
    text: string;
    /**
     * Value for the option.
     */
    value: T;
  }

  /**
   * Interface for an user privileges for an attribute.
   */
  interface UserPrivilege {
    /**
     * Specificies if the user can read data values for the attribute.
     */
    canRead: boolean;
    /**
     * Specificies if the user can update data values for the attribute.
     */
    canUpdate: boolean;
    /**
     * Specificies if the user can create data values for the attribute.
     */
    canCreate: boolean;
  }

  /**
   * Interface for an entity reference for the Xrm.Page context.
   */
  interface EntityReference<T extends string|number> {
    id: string;
    entityType: T;
    name?: string | null;
  }

  /**
   * Interface of the base functionality of a collection without the 'get' function.
   */
  interface CollectionBase<T> {
    /**
     * Apply an action in a delegate function to each object in the collection.
     *
     * @param delegate The delegate function which iterates over the collection.
     */
    forEach(delegate: ForEach<T>): void;

    /**
     * Get the number of items in the collection.
     */
    getLength(): number;
  }

  interface ForEach<T> {
    /**
     * Iterates over the collection.
     *
     * @param item The current object.
     * @param index The index of the current object.
     */
    (item: T, index: number): any; //eslint-disable-line @typescript-eslint/no-explicit-any
  }

  /**
   * A collection of a certain type.
   */
  interface Collection<T> extends CollectionBase<T> {
    /**
     * Get all the objects from the collection.
     */
    get(): T[];

    /**
     * Gets the object with the given index in the collection.
     *
     * @param index The index of the desired object.
     */
    get(index: number): T;

    /**
     * Gets the object with the given name in the collection.
     *
     * @param name The name of the desired object.
     */
    get(name: string): T;

    /**
     * Get the objects from the collection which make the delegate function return true.
     *
     * @param chooser The delegate function that filters the objects.
     */
    get(chooser: CollectionChooser<T>): T[];
  }

  interface CollectionChooser<T> {
    /**
     * Delegate function to choose which objects from the collections should be returned.
     *
     * @param item Current object
     * @parem index Index of the current object
     */
    (item: T, index: number): boolean;
  }

  /**
   * A collection of attributes.
   */
  interface AttributeCollection extends Collection<Attribute<any>> {} //eslint-disable-line @typescript-eslint/no-empty-interface, @typescript-eslint/no-explicit-any

  /**
   * A collection of controls.
   */

  interface ControlCollection extends Collection<AnyControl> {} //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of sections.
   */
  interface SectionCollection extends Collection<PageSection> {} //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of tabs.
   */
  interface TabCollection extends Collection<PageTab<SectionCollection>> { } //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of attributes.
   */
  interface AttributeCollectionBase extends CollectionBase<Attribute<any>> {} //eslint-disable-line @typescript-eslint/no-empty-interface, @typescript-eslint/no-explicit-any

  /**
   * A collection of controls.
   */
  interface ControlCollectionBase extends CollectionBase<AnyControl> {} //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of sections.
   */
  interface SectionCollectionBase extends CollectionBase<PageSection> {} //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of tabs.
   */
  interface TabCollectionBase extends CollectionBase<PageTab<SectionCollectionBase>> { } //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of QuickViewForms.
   */
  interface QuickViewFormCollection extends Collection<QuickViewFormBase> { } //eslint-disable-line @typescript-eslint/no-empty-interface

  /**
   * A collection of QuickViewForms
  */
  interface QuickViewFormCollectionBase extends CollectionBase<QuickViewFormBase> { } //eslint-disable-line @typescript-eslint/no-empty-interface

  interface QuickViewForm<T extends TabCollectionBase, C extends ControlCollectionBase> { } //eslint-disable-line @typescript-eslint/no-empty-interface, @typescript-eslint/no-unused-vars
  interface QuickViewFormBase extends QuickViewForm<TabCollectionBase, ControlCollectionBase> { } //eslint-disable-line @typescript-eslint/no-empty-interface

  type AttributeType = "boolean" | "datetime" | "decimal" | "double" | "integer" | "lookup" | "memo" | "money" | "optionset" | "string" | "multiselectoptionset";

  type AttributeFormat = "date" | "datetime" | "duration" | "email" | "language" | "none" | "phone" | "text" | "textarea" | "tickersymbol" | "timezone" | "url";

  type AttributeRequiredLevel = "none" | "required" | "recommended";

  type AttributeSubmitMode = "always" | "never" | "dirty";

  /**
   * Interface for an standard entity attribute.
   */
  interface Attribute<T> {
    /**
     * Collection of controls associated with the attribute.
     */
    controls: Collection<Control<Attribute<T>>>;

    /**
     * Retrieves the data value for an attribute.
     */
    getValue(): T | null;

    /**
     * Sets the data value for an attribute.
     *
     * @param val The new value for the attribute.
     */
    setValue(val?: T | null): void;

    /**
     * Get the type of attribute.
     */
    getAttributeType(): AttributeType;

    /**
     * Get the attribute format.
     */
    getFormat(): AttributeFormat;

    /**
     * Determine whether the value of an attribute has changed since it was last saved.
     */
    getIsDirty(): boolean;

    /**
     * Get the maximum length of string which an attribute that stores string data can have.
     */
    getMaxLength(): number;

    /**
     * Get the name of the attribute.
     */
    getName(): string;

    /**
     * Get a reference to the Xrm.Page.data.entity object that is the parent to all attributes.
     */
    getParent(): PageEntity<Collection<Attribute<any>>>; //eslint-disable-line @typescript-eslint/no-explicit-any

    /**
     * Returns an object with three Boolean properties corresponding to privileges indicating if the user can create,
     * read or update data values for an attribute. This function is intended for use when Field Level Security
     * modifies a user's privileges for a particular attribute.
     */
    getUserPrivilege(): UserPrivilege;

    /**
     * Sets a function to be called when the attribute value is changed.
     *
     * @param functionRef The event handler for the on change event.
     */
    addOnChange(functionRef: (context?: ExecutionContext<this, undefined>) => any): void; //eslint-disable-line @typescript-eslint/no-explicit-any

    /**
     * Removes a function from the OnChange event hander for an attribute.
     *
     * @param functionRef The event handler for the on change event.
     */
    removeOnChange(functionRef: Function): void; //eslint-disable-line @typescript-eslint/ban-types

    /**
     * Causes the OnChange event to occur on the attribute so that any script associated to that event can execute.
     */
    fireOnChange(): void;

    /**
     * Returns a string value indicating whether a value for the attribute is required or recommended.
     */
    getRequiredLevel(): AttributeRequiredLevel;

    /**
     * Sets whether data is required, recommended or optional for the attribute before the record can be saved.
     */
    setRequiredLevel(level: AttributeRequiredLevel): void;

    /**
     * Returns a string indicating when data from the attribute will be submitted when the record is saved.
     */
    getSubmitMode(): AttributeSubmitMode;

    /**
     * Sets when data from the attribute will be submitted when the record is saved.
     */
    setSubmitMode(mode: AttributeSubmitMode): void;
  }

  /**
   * Interface for a numerical attribute.
   */
  interface NumberAttribute extends Attribute<number> {
    /**
     * Collection of controls associated with the attribute.
     */
    controls: Collection<NumberControl>;

    /**
     * Returns a number indicating the maximum allowed value for an attribute.
     */
    getMax(): number;

    /**
     * Returns a number indicating the minimum allowed value for an attribute.
     */
    getMin(): number;

    /**
     * Returns the number of digits allowed to the right of the decimal point.
     */
    getPrecision(): number;
  }

  /**
   * Interface for a lookup attribute.
   */
  interface LookupAttribute<T extends string> extends Attribute<EntityReference<T>[]> {
    /**
     * Collection of controls associated with the attribute.
     */
    controls: Collection<LookupControl<T>>;
  }

  /**
   * Interface for a date attribute.
   */
  interface DateAttribute extends Attribute<Date> {
    /**
     * Collection of controls associated with the attribute.
     */
    controls: Collection<DateControl>;
  }

  /**
   * Interface for an OptionSet attribute.
   */
  interface OptionSetAttribute<T> extends Attribute<T> {
    /**
     * Collection of controls associated with the attribute.
     */
    controls: Collection<OptionSetControl<T>>;

    /**
     * Returns a value that represents the value set for an OptionSet or Boolean attribute when the form opened.
     */
    getInitialValue(): T | null;

    /**
     * Returns a string value of the text for the currently selected option for an optionset attribute.
     */
    getText(): string;

    /**
     * Returns an option object with the value matching the argument passed to the method.
     */
    getOption(value: string): Option<T> | null;

    /**
     * Returns an option object with the value matching the argument passed to the method.
     */
    getOption(value: T): Option<T> | null;

    /**
     * Returns an array of option objects representing the valid options for an option-set attribute.
     */
    getOptions(): Option<T>[];

    /**
     * Returns the option object that is selected in an optionset attribute.
     */
    getSelectedOption(): Option<T> | null;
  }

  type ControlType = "standard" | "iframe" | "lookup" | "optionset" | "subgrid" | "webresource" | "notes" | "timercontrol" | "kbsearch" | "multiselectoptionset";

  /**
   * Interface for a standard form control.
   */
  interface BaseControl {
    /**
     * Get information about the type of control.
     */
    getControlType(): ControlType;

    /**
     * Sets the focus on the control.
     */
    setFocus(): void;

    /**
     * Get the section object that the control is in.
     */
    getParent(): PageSection;

    /**
     * Get the name of the control.
     */
    getName(): string;

    /**
     * Returns the label for the control.
     */
    getLabel(): string;

    /**
     * Sets the label for the control.
     *
     * @param label The new label for the control.
     */
    setLabel(label: string): void;

    /**
     * Returns a value that indicates whether the control is currently visible.
     */
    getVisible(): boolean;

    /**
     * Sets a value that indicates whether the control is visible.
     *
     * @param visible True if the control should be visible; otherwise, false.
     */
    setVisible(visible: boolean): void;
  }

  //eslint-disable-next-line @typescript-eslint/no-explicit-any
  interface Control<T extends Xrm.Attribute<any>> extends BaseControl {
    /**
     * Get the attribute this control is bound to.
     */
    getAttribute(): T;

    /**
     * Returns whether the control is disabled.
     */
    getDisabled(): boolean;

    /**
     * Sets whether the control is disabled.
     *
     * @param disable True if the control should be disabled, otherwise false.
     */
    setDisabled(disable: boolean): void;
  }

  /**
   * Interface for an OptionSet form control.
   */
  interface OptionSetControl<T> extends Control<OptionSetAttribute<T>> {
    /**
     * Adds an option to an option set control.
     *
     * @param option An option object to add to the OptionSet.
     * @param index The index position to place the new option in. If not provided, the option will be added to the end.
     */
    addOption(option: Option<T>, index?: number): void;

    /**
     * Clears all options from an option set control.
     */
    clearOptions(): void;

    /**
     * Removes an option from an option set control.
     *
     * @param number The value of the option you want to remove.
     */
    removeOption(number: number): void;
  }

  /**
   * Interface for an external form control.
   */
  interface ExternalControl extends BaseControl {
    /**
     * Returns the object in the form that represents an IFRAME or WebResource.
     */
    getObject(): any; //eslint-disable-line @typescript-eslint/no-explicit-any

    /**
     * Returns the current URL being displayed in an IFRAME or WebResource.
     */
    getSrc(): string;

    /**
     * Sets the URL to be displayed in an IFRAME or WebResource.
     *
     * @param url The URL.
     */
    setSrc(url?: string): void;
  }

  /**
   * Interface for a WebResource form control.
   */
  interface WebResourceControl extends ExternalControl {
    /**
     * Returns the value of the data query string parameter passed to a web resource.
     */
    getData(): string;

    /**
     * Sets the value of the data query string parameter passed to a web resource.
     *
     * @param dataQuery The data value to pass to the web resource.
     */
    setData(dataQuery?: string): void;
  }

  /**
   * Interface for an IFrame form control.
   */
  interface IFrameControl extends ExternalControl {
    /**
     * Returns the default URL that an IFRAME control is configured to display. This method is not available for web resources.
     */
    getInitialUrl(): string;
  }

  /**
   * Interface for a DateTime form control.
   */
  //eslint-disable-next-line @typescript-eslint/no-empty-interface
  interface DateControl extends Control<Attribute<Date>> {}

  /**
   * Interface for a Lookup form control.
   */
  interface LookupControl<T extends string> extends Control<LookupAttribute<T>> {
    /**
     * Adds a new view for the lookup dialog box.
     *
     * @param viewId The string representation of a GUID for a view.
     * @param entityName The name of the entity.
     * @param viewDisplayName The name of the view.
     * @param fetchXml The fetchXml query for the view.
     * @param layoutXml The XML that defines the layout of the view.
     * @param isDefault Whether the view should be the default view.
     */
    addCustomView(viewId: string, entityName: string, viewDisplayName: string, fetchXml: string, layoutXml: string, isDefault: boolean): void;

    /**
     * Returns the ID value of the default lookup dialog view.
     */
    getDefaultView(): string;

    /**
     * Sets the default view for the lookup control dialog box.
     */
    setDefaultView(guid: string): void;
  }

  /**
   * Interface for a SubGrid form control.
   */
  //eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface SubGridControl<T extends string> extends BaseControl {
    /**
     * Refreshes the data displayed in a subgrid.
     */
    refresh(): void;
  }

  /**
   * Type to be be used for iterating over a list of controls and being able to interact with all of them with precursory checks for undefined
   */
  //eslint-disable-next-line @typescript-eslint/no-explicit-any
  type AnyControl = BaseControl & Partial<Control<any> & WebResourceControl & IFrameControl & LookupControl<string> & SubGridControl<string> & DateControl & OptionSetControl<any>>;
  
  const enum ViewTypeNumber {
    SavedQuery = 1039,
    UserQuery = 4230,
  }

  /**
   * Remarks:
   * If the subgrid control is not configured to display the view selector, calling this method on the ViewSelector returned by the GridControl.getViewSelector will throw an error.
   */
  interface ViewSelector {
    /**
     * Use this method to get a reference to the current view.
     */
    getCurrentView(): EntityReference<ViewTypeNumber>;

    /**
     * Use this method to determine whether the view selector is visible.
     */
    isVisible(): boolean;

    /**
     * Use this method to set the current view.
     */
    setCurrentView(reference: EntityReference<ViewTypeNumber>): void;
  }

  /**
   * Interface for a string form control.
   */
  //eslint-disable-next-line @typescript-eslint/no-empty-interface
  interface StringControl extends Control<Attribute<string>> {}

  /**
   * Interface for a number form control.
   */
  //eslint-disable-next-line @typescript-eslint/no-empty-interface
  interface NumberControl extends Control<NumberAttribute> {}

  /**
   * Interface for the entity on a form.
   */
  interface PageEntity<T extends AttributeCollectionBase> {
    /**
     * The collection of attributes for the entity.
     */
    attributes: T;

    /**
     * Adds a function to be called when the record is saved.
     *
     * @param functionRef Reference to a function. It will be added to the bottom of the event handler pipeline.
     *                  The execution context is automatically set to be passed as the first parameter passed to event handlers set using this method.
     */
    //eslint-disable-next-line @typescript-eslint/no-explicit-any
    addOnSave(functionRef: (context?: SaveEventContext<this>) => any): void;

    /**
     * Removes a function to be called when the record is saved.
     *
     * @param functionRef Reference to a function that was added to the OnSave event.
     */
    removeOnSave(functionRef: Function): void; //eslint-disable-line @typescript-eslint/ban-types

    /**
     * Returns a string representing the GUID id value for the record.
     */
    getId(): string;

    /**
     * Returns a string representing the xml that will be sent to the server when the record is saved.
     */
    getDataXml(): string;

    /**
     * Returns a string representing the logical name of the entity for the record.
     */
    getEntityName(): string;

    /**
     * Returns a Boolean value that indicates if any fields in the form have been modified.
     */
    getIsDirty(): boolean;
  }

  interface ExecutionContext<TSource, TArgs> {
    /**
     * Method that returns the Client-side context object
     */
    getContext(): context;

    /**
     * Method that returns a value that indicates the order in which this handler is executed.
     */
    getDepth(): number;

    /**
     * Method that returns an object with methods to manage the Save event.
     */
    getEventArgs(): TArgs;

    /**
     * Method that returns a reference to the object that the event occurred on.
     */
    getEventSource(): TSource;

    /**
     * Sets the value of a variable to be used by a handler after the current handler completes.
     *
     * @param key Key for the value
     * @param value The value to be stored
     */
    setSharedVariable(key: string, value: any): void; //eslint-disable-line @typescript-eslint/no-explicit-any

    /**
     * Retrieves a variable set using setSharedVariable.
     *
     * @param key Key for the desired value
     */
    getSharedVariable(key: string): any; //eslint-disable-line @typescript-eslint/no-explicit-any
  }

  //eslint-disable-next-line @typescript-eslint/no-empty-interface
  interface SaveEventContext<T> extends ExecutionContext<T, SaveEventArgs> { }

  interface SaveEventArgs {
    /**
     * Returns a value indicating how the save event was initiated by the user.
     */
    getSaveMode(): SaveMode;

    /**
     * Returns a value indicating whether the save event has been canceled because the preventDefault method was used in this event hander or a previous event handler.
     */
    isDefaultPrevented(): boolean;

    /**
     * Cancels the save operation, but all remaining handlers for the event will still be executed.
     */
    preventDefault(): void;
  }

  /**
   * Supported values returned to detect different ways entity records may be saved by the user.
   */
  const enum SaveMode {
    Save = 1,
    SaveAndClose = 2,
    SaveAndNew = 59,
    AutoSave = 70,
    SaveAsCompleted = 58,
    Deactivate = 5,
    Reactivate = 6,
    Assign = 47,
    Send = 7,
    Qualify = 16,
    Disqualify = 15,
  }

  /**
   * Interface for the data of a form.
   */
  interface DataModule<T extends AttributeCollectionBase> {
    /**
     * Contains information about the entity of the page.
     */
    entity: PageEntity<T>;
  }

  interface ProcessContainer {
    [id: string]: string;
  }

  interface Process {
    /**
     * Returns the unique identifier of the process.
     */
    getId(): string;

    /**
     * Returns the name of the process.
     */
    getName(): string;

    /**
     * Returns an collection of stages in the process.
     */
    getStages(): Collection<Stage>;

    /**
     * Returns true if the process is rendered, false if not.
     */
    isRendered(): boolean;
  }

  type StageStatus = "active" | "inactive";

  interface Stage {
    /**
     * Returns an object with a getValue method which will return the integer value of the business process flow category.
     */
    getCategory(): IStageCategory;

    /**
     * Returns the logical name of the entity associated with the stage.
     */
    getEntityName(): string;

    /**
     * Returns the unique identifier of the stage.
     */
    getId(): string;

    /**
     * Returns the name of the stage.
     */
    getName(): string;

    /**
     * Returns the status of the stage.
     */
    getStatus(): StageStatus;

    /**
     * Returns a collection of steps in the stage.
     */
    getSteps(): StageStep[];
  }

  const enum StageCategory {
    Qualify = 0,
    Develop = 1,
    Propose = 2,
    Close = 3,
    Identify = 4,
    Research = 5,
    Resolve = 6,
  }

  interface IStageCategory {
    /**
     * Returns the stage category.
     */
    getValue(): StageCategory;
  }

  interface StageStep {
    /**
     * Returns the logical name of the attribute associated to the step.
     */
    getAttribute(): string;

    /**
     * Returns the name of the step.
     */
    getName(): string;

    /**
     * Returns whether the step is required in the business process flow.
     */
    isRequired(): boolean;
  }

  interface SaveOptions {
    /**
     * (Optional) Boolean. Indicate whether to use the Book or Reschedule messages rather than the Create or Update messages.
     * This option is only applicable when used with appointment, recurring appointment, or service activity records.
     */
    useSchedulingEngine?: boolean;
  }

  interface Then<T> {
    /**
     * A function which can add callback handlers after it has finished.
     *
     * @param successCallback A function to call when the operation succeeds.
     * @param errorCallback A function to call when the operation fails.
     */
    then(successCallback?: (result: T) => void, errorCallback?: ErrorCallback): void;
  }

  interface ErrorCallback {
    /**
     * A function to call when the operation fails.
     *
     * @param messageObject Object containing information about the error.
     */
    (messageObject: ErrorCallbackObject): void;

    /**
     * A function to call when the operation fails.
     *
     * @param errorCode The error code.
     * @param message A localized error message.
     */
    (errorCode: number, message: string): void;
  }

  interface ErrorCallbackObject {
    errorCode: number;
    message: string;
  }

  /**
   * Interface for a section on a form.
   */
  interface PageSection {
    /**
     * A collection of controls in the section.
     */
    controls: Collection<AnyControl>;

    /**
     * Method to return the name of the section.
     */
    getName(): string;

    /**
     * Method to return the tab containing the section.
     */
    getParent(): PageTab<Collection<PageSection>>;

    /**
     * Returns the label for the section.
     */
    getLabel(): string;

    /**
     * Sets the label for the section.
     *
     * @param label The label text to set.
     */
    setLabel(label: string): void;

    /**
     * Sets a value to show or hide the section.
     */
    setVisible(visibility: boolean): void;

    /**
     * Returns true if the section is visible, otherwise returns false.
     */
    getVisible(): boolean;
  }

  type CollapsableDisplayState = "expanded" | "collapsed";

  /**
   * Interface for a tab on a form.
   */
  interface PageTab<T extends SectionCollectionBase> {
    /**
     * Collection of sections within this tab.
     */
    sections: T;

    /**
     * Method to get the name of the tab.
     */
    getName(): string;

    /**
     * Returns a value that indicates whether the tab is collapsed or expanded.
     */
    getDisplayState(): CollapsableDisplayState;

    /**
     * Sets the tab to be collapsed or expanded.
     */
    setDisplayState(state: CollapsableDisplayState): void;

    /**
     * Returns the Xrm.Page.ui object.
     */
    getParent(): UiModule<Collection<PageTab<Collection<PageSection>>>, Collection<BaseControl>, Collection<QuickViewFormBase>>;

    /**
     * Returns the tab label.
     */
    getLabel(): string;

    /**
     * Sets the label for the tab.
     *
     * @param label The new label for the tab.
     */
    setLabel(label: string): void;

    /**
     * Sets the focus on the tab.
     */
    setFocus(): void;

    /**
     * Sets a value that indicates whether the control is visible.
     */
    setVisible(visibility: boolean): void;

    /**
     * Returns a value that indicates whether the tab is visible.
     */
    getVisible(): boolean;
  }

  type NotificationLevel = "INFO" | "WARNING" | "ERROR";

  /**
   * Interface for the ui of a form.
   */
  //eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface UiModule<T extends TabCollectionBase, C extends ControlCollectionBase, Q extends QuickViewFormCollectionBase = QuickViewFormCollectionBase> {
    /**
     * Collection of tabs on the page.
     */
    tabs: T;

    /**
     * Collection of controls on the page.
     */
    controls: C;

    /**
     * Navigation for the page.
     */
    navigation: Navigation;

    /**
     * Use the formSelector.getCurrentItem method to retrieve information about the form currently in use and the formSelector.items
     * collection containing information about all the forms available for the user.
     */
    formSelector: FormSelector;

    /**
     * Method to get the form context for the record.
     * Matches the values found in the Xrm.FormType enum.
     */
    getFormType(): FormType;

    /**
     * Method to close the form.
     */
    close(): void;

    /**
     * Method to get the control object that currently has focus on the form. Web Resource and IFRAME controls are not returned by this method.
     * This method was deprecated in Microsoft Dynamics CRM 2013 Update Rollup 2.
     */
    getCurrentControl(): AnyControl;

    /**
     * Method to cause the ribbon to re-evaluate data that controls what is displayed in it.
     */
    refreshRibbon(): void;

    /**
     * Method to get the height of the viewport in pixels.
     */
    getViewPortHeight(): number;

    /**
     * Method to get the width of the viewport in pixels.
     */
    getViewPortWidth(): number;
  }

  interface FormSelector {
    /**
     * Method to return a reference to the form currently being shown.
     */
    getCurrentItem(): FormItem;

    /**
     * Method to return a reference to the form currently being shown.
     */
    items: Collection<FormItem>;
  }

  interface FormItem {
    /**
     * Returns the GUID ID of the form.
     */
    getId(): string;

    /**
     * Returns the label for the form.
     */
    getLabel(): string;

    /**
     * Opens the specified form.
     */
    navigate(): void;
  }

  interface Navigation {
    /**
     * Navigation items for the page.
     */
    items: Collection<NavigationItem>;
  }

  interface NavigationItem {
    /**
     * Returns the name of the item.
     */
    getId(): string;

    /**
     * Returns the label for the item.
     */
    getLabel(): string;

    /**
     * Sets the label for the item.
     * @param label The new label for the item.
     */
    setLabel(label: string): void;

    /**
     * Sets the focus on the item.
     */
    setFocus(): string;

    /**
     * Returns a value that indicates whether the item is currently visible.
     * Returns true if the item is visible; false otherwise..
     */
    getVisible(): boolean;

    /**
     * Sets a value that indicates whether the item is visible.
     * @param visible Specify true or false to indicate whether the item is visible or not.
     */
    setVisible(visible: boolean): void;
  }

  /**
   * Interface for the context of a form.
   */
  interface context {
    /**
     * Returns the base URL that was used to access the application.
     */
    getClientUrl(): string;

    /**
     * Returns a string representing the current Microsoft Office Outlook theme chosen by the user.
     */
    getCurrentTheme(): string;

    /**
     * Prepends the organization name to the specified path.
     */
    prependOrgName(sPath: string): string;
  }

  /**
   * Interface for the base of an Xrm.Page
   */
  interface PageBase<A extends AttributeCollectionBase, T extends TabCollectionBase, C extends ControlCollectionBase, Q extends QuickViewFormCollectionBase = QuickViewFormCollectionBase> {
    /**
     * Data on the page.
     */
    data: Xrm.DataModule<A>;

    /**
     * UI of the page.
     */
    ui: Xrm.UiModule<T, C, Q>;

    /**
     * Returns string with current page URL.
     */
    getUrl(): string;
    }

  /**
   * Interface for a generic Xrm.Page
   */
  interface BasicPage extends PageBase<AttributeCollection, TabCollection, ControlCollection> {
    /**
     * Generic getAttribute
     */
    getAttribute(attrName: string): Xrm.Attribute<any> | undefined; //eslint-disable-line @typescript-eslint/no-explicit-any

    /**
     * Generic getControl
     */
    getControl(ctrlName: string): Xrm.AnyControl | undefined;
  }
}

type BaseXrm = typeof Xrm;
/**
 * Client-side xRM object model.
 */
//eslint-disable-next-line @typescript-eslint/no-unused-vars
interface Xrm<T extends Xrm.PageBase<Xrm.AttributeCollectionBase, Xrm.TabCollectionBase, Xrm.ControlCollectionBase, Xrm.QuickViewFormCollectionBase>> extends BaseXrm {
  /**
   * Various utility functions can be found here.
   */
  Utility: Xrm.Utility;
}

declare namespace Xrm {
  let Utility: Utility;

  /**
   * Interface for a Lookup which is used by some Xrm.Utility functions.
   */
  interface Lookup {
    /**
     * Entity type (logical name) of the lookup.
     */
    entityType: string;

    /**
     * GUID of the lookup.
     */
    id: string;

    /**
     * Record name of the lookup.
     */
    name?: string;
  }

  /**
   * Interface for a WindowOption object.
   */
  interface WindowOptions {
    /**
     * Specifies if it should open in a new window.
     */
    openInNewWindow: boolean;
  }
}

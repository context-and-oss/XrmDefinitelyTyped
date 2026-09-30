/// <reference types="xrm" />
import "@delegateas/xrmquery";

export {};

declare global {
  namespace XDTForm {
    interface LookupValue<TTarget extends string = string> extends Xrm.LookupValue {
      entityType: TTarget;
    }

    interface LookupAttribute<TTarget extends string = string> extends Xrm.Attributes.LookupAttribute {
      getValue(): LookupValue<TTarget>[] | null;
      setValue(value: LookupValue<TTarget>[] | null): void;
    }

    interface LookupControl<TTarget extends string = string> extends Xrm.Controls.LookupControl {
      getAttribute(): LookupAttribute<TTarget>;
    }

    interface OptionSetControl<TValue extends number = number> extends Xrm.Controls.OptionSetControl {
      getAttribute(): Xrm.Attributes.OptionSetAttribute<TValue>;
    }

    interface BooleanControl extends Xrm.Controls.BooleanControl {
      getAttribute(): Xrm.Attributes.BooleanAttribute;
    }

    interface MultiSelectOptionSetControl<TValue extends number = number>
      extends Xrm.Controls.MultiSelectOptionSetControl {
      getAttribute(): Xrm.Attributes.MultiSelectOptionSetAttribute<TValue>;
    }

    interface SubGridControl<TEntity extends string = string> extends Xrm.Controls.GridControl {}

    interface AttributeCollectionBase
      extends Xrm.Collection.ItemCollection<Xrm.Attributes.Attribute> {}

    interface ControlCollectionBase
      extends Xrm.Collection.ItemCollection<Xrm.Controls.Control> {}

    interface SectionCollectionBase
      extends Xrm.Collection.ItemCollection<Xrm.Controls.Section> {}

    interface TabCollectionBase
      extends Xrm.Collection.ItemCollection<Xrm.Controls.Tab> {}

    interface QuickViewFormCollectionBase
      extends Xrm.Collection.ItemCollection<Xrm.Controls.QuickFormControl> {}

    interface PageTab<TSections extends SectionCollectionBase = SectionCollectionBase>
      extends Xrm.Controls.Tab {
      sections: TSections;
    }

    interface TypedEntity<TAttributes extends AttributeCollectionBase> extends Xrm.Entity {
      attributes: TAttributes;
    }

    interface TypedData<TAttributes extends AttributeCollectionBase> extends Xrm.Data {
      entity: TypedEntity<TAttributes>;
    }

    interface TypedUi<
      TTabs extends TabCollectionBase,
      TControls extends ControlCollectionBase,
      TQuickViews extends QuickViewFormCollectionBase,
    > extends Xrm.Ui {
      tabs: TTabs;
      controls: TControls;
      quickForms: TQuickViews;
    }

    interface FormContextBase<
      TAttributes extends AttributeCollectionBase,
      TTabs extends TabCollectionBase,
      TControls extends ControlCollectionBase,
      TQuickViews extends QuickViewFormCollectionBase = QuickViewFormCollectionBase,
    > extends Xrm.FormContext {
      data: TypedData<TAttributes>;
      ui: TypedUi<TTabs, TControls, TQuickViews>;
    }

    type QuickViewControlBase = Omit<Xrm.Controls.QuickFormControl, "getControl">;

    interface QuickViewForm<
      TTabs extends TabCollectionBase,
      TControls extends ControlCollectionBase,
    > extends QuickViewControlBase {}

    type QuickViewFormBase = Xrm.Controls.QuickFormControl;
    type WebResourceControl = Xrm.Controls.FramedControl;
  }
}

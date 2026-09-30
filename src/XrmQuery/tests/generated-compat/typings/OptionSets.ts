export const account_accountcategorycode = {
  PreferredCustomer: 1,
  Standard: 2,
} as const;

export type account_accountcategorycode =
  (typeof account_accountcategorycode)[keyof typeof account_accountcategorycode];

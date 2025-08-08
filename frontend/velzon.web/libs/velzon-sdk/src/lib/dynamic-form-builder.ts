import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FormFieldOptions, getFormMetadata } from './core/models/form-field.decorator';

export class DynamicFormBuilder<T extends object> {
  private metadata: Record<keyof T, FormFieldOptions>;

  constructor(private modelClass: new () => T, private fb: FormBuilder) {
    this.metadata = getFormMetadata(modelClass);
  }

  /** Cria um FormGroup com base nos decorators do modelo */
  buildForm(): FormGroup {
    const group: any = {};
    
    for (const key in this.metadata) {
      const field = this.metadata[key];
      const validators = [];
      if (field.required) validators.push(Validators.required);
      if (field.minLength) validators.push(Validators.minLength(field.minLength));
      if (field.maxLength) validators.push(Validators.maxLength(field.maxLength));
      group[key] = this.fb.control('', validators);
    }

    return this.fb.group(group);
  }

  /** Agrupa os campos por aba (tab) */
  groupFieldsByTab(): Record<string, string[]> {
    const grouped: Record<string, string[]> = {};
    for (const key in this.metadata) {
      const tab = this.metadata[key].tab || 'Geral';
      if (!grouped[tab]) grouped[tab] = [];
      grouped[tab].push(key as string);
    }
    return grouped;
  }

  /** Retorna o metadata */
  getMetadata(): Record<string, FormFieldOptions> {
    return this.metadata;
  }
}

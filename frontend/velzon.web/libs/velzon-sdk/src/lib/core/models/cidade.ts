import { FormField } from './form-field.decorator';

export class  SmaCidade {
  @FormField({ label: 'Código', type: 'number', required: true})
  idCidade: number;
  @FormField({
    label: 'U.F.',
    type: 'autocomplete',
    //tab: 'Preferências',
    options: [
      { label: 'São Paulo', value: 'sp' },
      { label: 'Rio de Janeiro', value: 'rj' },
      { label: 'Belo Horizonte', value: 'bh' }
    ]
  })
  idUf: number;
  @FormField({ label: 'Nome', type: 'text', required: true})
  nomeCidade: string;
  capital: boolean;
  idMesoRegiao: string;
  idMicroRegiao: string;
}

export class AccessCredentials
{
    constructor(userName: string, password: string)
    {
        this.Email = userName;
        this.AccessKey = password;
        this.GrantType = 'password';
    }

    Email: string;
    AccessKey: string;
    GrantType: string;  

}

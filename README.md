I'm not quite used to the traditional ACME (Auto Cert Mgmt Env).
So, I'm building one myself.

I plan to build one using MEF.

                                SSH Targets
                                    |
             IIS Target (How?) - Targets - SoftEther Target
                                    |
                                  Kernel ------------ DNSProviders --- Tencent Cloud DNS Provider
                                    |                      |
      Tencent Cloud Provider - CertProviders       Aliyun DNS Provider
                                    |
                              ACME Providers


Milestone1: It just works. (Achieved)

Milestone2: Run as service.

Milestone3: Managed Extensibility Framework (MEF) for Certificate Providers. (Long term goal)

TODO:
- Aliyun Certificate Provider. (I currently does not have a domain using Aliyun DNS.)
- Separated DNS Provider and Cert Provider support.
- Paid certificate from TencentCloud and Aliyun. (I don't have money for that.)
- Other certificate providers. (I'm not planning currently)
- IIS Target Support. (How to do that?)
- Complicated SSH support.

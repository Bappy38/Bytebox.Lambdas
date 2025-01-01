
### Deployment Pipeline Setup

- Create a lambda on AWS console
- Create a policy with necessary permission on the created lambda

    ```

    {
        "Version": "2012-10-17",
        "Statement": [
            {
                "Sid": "VisualEditor0",
                "Effect": "Allow",
                "Action": "lambda:UpdateFunctionCode",
                "Resource": "{{Lambda-ARN}}"
            }
        ]
    }
    
    ```

- Create an IAM User by attaching the policy we created earlier
- Create github repository and from `Settings -> Secrets and Variables -> Actions -> `, create two secrets `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` by providing the credentials of created user
- Now push the function code to the repository along with the pipeline. The pipeline will deploy the updated function code.
- We can also extend our pipeline if we want to add more functions in this solution. We just have to add Build and Deployment steps of newly created function.